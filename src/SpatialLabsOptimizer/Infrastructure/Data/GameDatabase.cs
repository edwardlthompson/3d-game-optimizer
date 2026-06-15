using Microsoft.Data.Sqlite;
using SpatialLabsOptimizer.Domain;

namespace SpatialLabsOptimizer.Infrastructure.Data;

public sealed partial class GameDatabase
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
