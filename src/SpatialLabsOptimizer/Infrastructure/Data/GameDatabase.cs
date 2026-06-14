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
            """;
        await cmd.ExecuteNonQueryAsync(cancellationToken);
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
                cover_cache_path = excluded.cover_cache_path,
                is_favorite = excluded.is_favorite
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
