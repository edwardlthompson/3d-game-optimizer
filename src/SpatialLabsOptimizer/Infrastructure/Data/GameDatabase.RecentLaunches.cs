namespace SpatialLabsOptimizer.Infrastructure.Data;

public sealed partial class GameDatabase
{
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
}
