namespace SpatialLabsOptimizer.Infrastructure.Data;

public sealed partial class GameDatabase
{
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
}
