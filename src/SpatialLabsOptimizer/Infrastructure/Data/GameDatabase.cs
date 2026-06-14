using Microsoft.Data.Sqlite;
using SpatialLabsOptimizer.Domain;

namespace SpatialLabsOptimizer.Infrastructure.Data;

public sealed class GameDatabase : IAsyncDisposable
{
    private readonly string _dbPath;
    private SqliteConnection? _connection;

    public GameDatabase(string? dbPath = null)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var folder = Path.Combine(appData, "3d-game-optimizer");
        Directory.CreateDirectory(folder);
        _dbPath = dbPath ?? Path.Combine(folder, "library.db");
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _connection = new SqliteConnection($"Data Source={_dbPath}");
        await _connection.OpenAsync(cancellationToken);

        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS games (
                steam_app_id INTEGER PRIMARY KEY,
                title TEXT NOT NULL,
                tier INTEGER NOT NULL,
                launch_readiness INTEGER NOT NULL,
                is_installed INTEGER NOT NULL DEFAULT 0,
                current_players INTEGER,
                peak_players INTEGER,
                review_score_percent INTEGER,
                review_count INTEGER,
                review_sort_score REAL,
                review_descriptor TEXT,
                cover_cache_path TEXT,
                is_favorite INTEGER DEFAULT 0,
                last_played_3d TEXT
            );
            CREATE INDEX IF NOT EXISTS idx_games_tier ON games(tier);
            CREATE INDEX IF NOT EXISTS idx_games_readiness ON games(launch_readiness);
            CREATE INDEX IF NOT EXISTS idx_games_review_sort ON games(review_sort_score);
            CREATE INDEX IF NOT EXISTS idx_games_current_players ON games(current_players);

            CREATE VIEW IF NOT EXISTS v_ready_to_play AS
                SELECT * FROM games
                WHERE is_installed = 1 AND launch_readiness IN (0, 1) AND tier <= 4;

            CREATE VIEW IF NOT EXISTS v_one_click_ready AS
                SELECT * FROM games
                WHERE is_installed = 1 AND launch_readiness = 0 AND tier <= 4;

            CREATE VIEW IF NOT EXISTS v_multiplayer_active AS
                SELECT * FROM games
                WHERE current_players >= 500;

            CREATE TABLE IF NOT EXISTS local_game_installs (
                stable_app_id INTEGER PRIMARY KEY,
                folder_path TEXT NOT NULL,
                launch_exe TEXT NOT NULL,
                display_title TEXT NOT NULL,
                last_scanned_at TEXT NOT NULL,
                is_stale INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS recent_launches (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                stable_app_id INTEGER NOT NULL,
                title TEXT NOT NULL,
                launched_at TEXT NOT NULL,
                success INTEGER NOT NULL,
                error_code TEXT
            );
            CREATE INDEX IF NOT EXISTS idx_recent_launches_time ON recent_launches(launched_at DESC);
            """;
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpsertLocalInstallAsync(
        int stableAppId,
        string folderPath,
        string launchExe,
        string displayTitle,
        bool isStale,
        CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(cancellationToken);
        await using var cmd = _connection!.CreateCommand();
        cmd.CommandText = """
            INSERT INTO local_game_installs (
                stable_app_id, folder_path, launch_exe, display_title, last_scanned_at, is_stale)
            VALUES ($id, $folder, $exe, $title, $scanned, $stale)
            ON CONFLICT(stable_app_id) DO UPDATE SET
                folder_path = excluded.folder_path,
                launch_exe = excluded.launch_exe,
                display_title = excluded.display_title,
                last_scanned_at = excluded.last_scanned_at,
                is_stale = excluded.is_stale
            """;
        cmd.Parameters.AddWithValue("$id", stableAppId);
        cmd.Parameters.AddWithValue("$folder", folderPath);
        cmd.Parameters.AddWithValue("$exe", launchExe);
        cmd.Parameters.AddWithValue("$title", displayTitle);
        cmd.Parameters.AddWithValue("$scanned", DateTimeOffset.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("$stale", isStale ? 1 : 0);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task MarkLocalInstallsStaleExceptAsync(
        IReadOnlyCollection<int> activeIds,
        CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(cancellationToken);
        await using var cmd = _connection!.CreateCommand();
        if (activeIds.Count == 0)
        {
            cmd.CommandText = "UPDATE local_game_installs SET is_stale = 1";
        }
        else
        {
            var placeholders = string.Join(',', activeIds.Select((_, i) => $"$id{i}"));
            cmd.CommandText = $"UPDATE local_game_installs SET is_stale = 1 WHERE stable_app_id NOT IN ({placeholders})";
            var index = 0;
            foreach (var id in activeIds)
            {
                cmd.Parameters.AddWithValue($"$id{index++}", id);
            }
        }

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LocalGameInstallRecord>> GetLocalInstallsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(cancellationToken);
        await using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT stable_app_id, folder_path, launch_exe, display_title, last_scanned_at, is_stale FROM local_game_installs";
        var items = new List<LocalGameInstallRecord>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new LocalGameInstallRecord(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                DateTimeOffset.Parse(reader.GetString(4)),
                reader.GetInt32(5) == 1));
        }

        return items;
    }

    public async Task<LocalGameInstallRecord?> GetLocalInstallAsync(int stableAppId, CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(cancellationToken);
        await using var cmd = _connection!.CreateCommand();
        cmd.CommandText = """
            SELECT stable_app_id, folder_path, launch_exe, display_title, last_scanned_at, is_stale
            FROM local_game_installs WHERE stable_app_id = $id
            """;
        cmd.Parameters.AddWithValue("$id", stableAppId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new LocalGameInstallRecord(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            DateTimeOffset.Parse(reader.GetString(4)),
            reader.GetInt32(5) == 1);
    }

    public async Task RecordRecentLaunchAsync(
        int stableAppId,
        string title,
        bool success,
        string? errorCode,
        CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(cancellationToken);
        await using var cmd = _connection!.CreateCommand();
        cmd.CommandText = """
            INSERT INTO recent_launches (stable_app_id, title, launched_at, success, error_code)
            VALUES ($id, $title, $at, $success, $error)
            """;
        cmd.Parameters.AddWithValue("$id", stableAppId);
        cmd.Parameters.AddWithValue("$title", title);
        cmd.Parameters.AddWithValue("$at", DateTimeOffset.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("$success", success ? 1 : 0);
        cmd.Parameters.AddWithValue("$error", errorCode ?? (object)DBNull.Value);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RecentLaunchRow>> GetRecentLaunchesAsync(int limit, CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(cancellationToken);
        await using var cmd = _connection!.CreateCommand();
        cmd.CommandText = """
            SELECT stable_app_id, title, launched_at, success, error_code
            FROM recent_launches ORDER BY launched_at DESC LIMIT $limit
            """;
        cmd.Parameters.AddWithValue("$limit", limit);
        var items = new List<RecentLaunchRow>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new RecentLaunchRow(
                reader.GetInt32(0),
                reader.GetString(1),
                DateTimeOffset.Parse(reader.GetString(2)),
                reader.GetInt32(3) == 1,
                reader.IsDBNull(4) ? null : reader.GetString(4)));
        }

        return items;
    }

    public async Task UpsertGameAsync(GameCatalogItem item, CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(cancellationToken);
        await using var cmd = _connection!.CreateCommand();
        cmd.CommandText = """
            INSERT INTO games (
                steam_app_id, title, tier, launch_readiness, is_installed,
                current_players, review_score_percent, review_count, review_sort_score,
                review_descriptor, cover_cache_path, is_favorite)
            VALUES ($id, $title, $tier, $readiness, $installed,
                $players, $reviewPct, $reviewCount, $reviewSort,
                $descriptor, $cover, $favorite)
            ON CONFLICT(steam_app_id) DO UPDATE SET
                title = excluded.title,
                tier = excluded.tier,
                launch_readiness = excluded.launch_readiness,
                is_installed = excluded.is_installed,
                current_players = excluded.current_players,
                review_score_percent = excluded.review_score_percent,
                review_count = excluded.review_count,
                review_sort_score = excluded.review_sort_score,
                review_descriptor = excluded.review_descriptor,
                cover_cache_path = excluded.cover_cache_path
            """;
        cmd.Parameters.AddWithValue("$id", item.SteamAppId);
        cmd.Parameters.AddWithValue("$title", item.Title);
        cmd.Parameters.AddWithValue("$tier", (int)item.Tier);
        cmd.Parameters.AddWithValue("$readiness", (int)item.Readiness);
        cmd.Parameters.AddWithValue("$installed", item.IsInstalled ? 1 : 0);
        cmd.Parameters.AddWithValue("$players", item.CurrentPlayers ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$reviewPct", item.ReviewScorePercent ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$reviewCount", item.ReviewCount ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$reviewSort", item.ReviewSortScore ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$descriptor", item.ReviewDescriptor ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$cover", item.CoverCachePath ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$favorite", item.IsFavorite ? 1 : 0);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task SetFavoriteAsync(int appId, bool isFavorite, CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(cancellationToken);
        await using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "UPDATE games SET is_favorite = $favorite WHERE steam_app_id = $id";
        cmd.Parameters.AddWithValue("$favorite", isFavorite ? 1 : 0);
        cmd.Parameters.AddWithValue("$id", appId);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<GameCatalogItem?> GetGameAsync(int appId, CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(cancellationToken);
        await using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT * FROM games WHERE steam_app_id = $id";
        cmd.Parameters.AddWithValue("$id", appId);
        var items = await ReadGamesAsync(cmd, cancellationToken);
        return items.FirstOrDefault();
    }

    public async Task<IReadOnlyList<GameCatalogItem>> GetReadyToPlayAsync(CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(cancellationToken);
        await using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT * FROM v_ready_to_play ORDER BY tier ASC, review_sort_score DESC";
        return await ReadGamesAsync(cmd, cancellationToken);
    }

    public async Task<IReadOnlyList<GameCatalogItem>> GetAllGamesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(cancellationToken);
        await using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT * FROM games ORDER BY title ASC";
        return await ReadGamesAsync(cmd, cancellationToken);
    }

    public async Task<int> CountGamesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(cancellationToken);
        await using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM games";
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    private static async Task<IReadOnlyList<GameCatalogItem>> ReadGamesAsync(
        SqliteCommand cmd,
        CancellationToken cancellationToken)
    {
        var items = new List<GameCatalogItem>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new GameCatalogItem(
                reader.GetInt32(0),
                reader.GetString(1),
                (CompatibilityTier)reader.GetInt32(2),
                (LaunchReadinessState)reader.GetInt32(3),
                reader.GetInt32(4) == 1,
                reader.IsDBNull(5) ? null : reader.GetInt32(5),
                reader.IsDBNull(7) ? null : reader.GetInt32(7),
                reader.IsDBNull(8) ? null : reader.GetInt32(8),
                reader.IsDBNull(9) ? null : reader.GetDouble(9),
                reader.IsDBNull(11) ? null : reader.GetString(11),
                reader.IsDBNull(10) ? null : reader.GetString(10),
                reader.GetInt32(12) == 1));
        }

        return items;
    }

    private async Task EnsureOpenAsync(CancellationToken cancellationToken)
    {
        if (_connection is null)
        {
            await InitializeAsync(cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}

public sealed record LocalGameInstallRecord(
    int StableAppId,
    string FolderPath,
    string LaunchExe,
    string DisplayTitle,
    DateTimeOffset LastScannedAt,
    bool IsStale);

public sealed record RecentLaunchRow(
    int StableAppId,
    string Title,
    DateTimeOffset LaunchedAt,
    bool Success,
    string? ErrorCode);
