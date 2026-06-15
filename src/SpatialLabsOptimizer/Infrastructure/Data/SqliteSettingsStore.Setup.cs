namespace SpatialLabsOptimizer.Infrastructure.Data;

public sealed partial class SqliteSettingsStore
{
    internal const string SetupCompletedAtKey = "setup_completed_at";
    internal const string ToolInstallPathPrefix = "tool_install_path_";

    public async Task<string?> GetActiveDisplayProfileIdAsync(CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(cancellationToken);
        await using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT profile_id FROM active_display WHERE id = 1";
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result as string;
    }

    public async Task SetActiveDisplayProfileIdAsync(string profileId, CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(cancellationToken);
        await using var cmd = _connection!.CreateCommand();
        cmd.CommandText = """
            INSERT INTO active_display (id, profile_id) VALUES (1, $profile)
            ON CONFLICT(id) DO UPDATE SET profile_id = excluded.profile_id
            """;
        cmd.Parameters.AddWithValue("$profile", profileId);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<DateTimeOffset?> GetSetupCompletedAtAsync(CancellationToken cancellationToken = default)
    {
        var value = await GetAsync(SetupCompletedAtKey, cancellationToken);
        return DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;
    }

    public async Task SetSetupCompletedAtAsync(DateTimeOffset completedAt, CancellationToken cancellationToken = default)
    {
        await SetAsync(SetupCompletedAtKey, completedAt.ToString("O"), cancellationToken);
    }

    public async Task<string?> GetToolInstallPathAsync(string toolId, CancellationToken cancellationToken = default)
    {
        return await GetAsync(ToolInstallPathPrefix + toolId, cancellationToken);
    }

    public async Task SetToolInstallPathAsync(string toolId, string path, CancellationToken cancellationToken = default)
    {
        await SetAsync(ToolInstallPathPrefix + toolId, path, cancellationToken);
    }
}
