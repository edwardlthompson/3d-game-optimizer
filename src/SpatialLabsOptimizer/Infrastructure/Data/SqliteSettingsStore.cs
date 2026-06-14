using Microsoft.Data.Sqlite;

namespace SpatialLabsOptimizer.Infrastructure.Data;

public sealed class SqliteSettingsStore : IAsyncDisposable
{
    private readonly string _dbPath;
    private SqliteConnection? _connection;

    public SqliteSettingsStore(string? dbPath = null)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var folder = Path.Combine(appData, "3d-game-optimizer");
        Directory.CreateDirectory(folder);
        _dbPath = dbPath ?? Path.Combine(folder, "settings.db");
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _connection = new SqliteConnection($"Data Source={_dbPath}");
        await _connection.OpenAsync(cancellationToken);

        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS settings (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS game_overrides (
                steam_app_id INTEGER PRIMARY KEY,
                depth REAL,
                convergence REAL,
                platform_override TEXT,
                is_favorite INTEGER DEFAULT 0
            );
            CREATE TABLE IF NOT EXISTS active_display (
                id INTEGER PRIMARY KEY CHECK (id = 1),
                profile_id TEXT NOT NULL
            );
            """;
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(cancellationToken);
        await using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT value FROM settings WHERE key = $key";
        cmd.Parameters.AddWithValue("$key", key);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result as string;
    }

    public async Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(cancellationToken);
        await using var cmd = _connection!.CreateCommand();
        cmd.CommandText = """
            INSERT INTO settings (key, value) VALUES ($key, $value)
            ON CONFLICT(key) DO UPDATE SET value = excluded.value
            """;
        cmd.Parameters.AddWithValue("$key", key);
        cmd.Parameters.AddWithValue("$value", value);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> GetDisclaimerAcceptedAsync(CancellationToken cancellationToken = default)
    {
        var value = await GetAsync("disclaimer_accepted", cancellationToken);
        return value == "true";
    }

    public async Task SetDisclaimerAcceptedAsync(bool accepted, CancellationToken cancellationToken = default)
    {
        await SetAsync("disclaimer_accepted", accepted ? "true" : "false", cancellationToken);
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
