using Microsoft.Data.Sqlite;
using SpatialLabsOptimizer.Domain;

namespace SpatialLabsOptimizer.Infrastructure.Data;

public sealed partial class GameDatabase
{
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

    public async Task<IReadOnlyList<GameCatalogItem>> GetCompatible3DAsync(CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(cancellationToken);
        await using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT * FROM v_compatible_3d ORDER BY tier ASC, review_sort_score DESC";
        return await ReadGamesAsync(cmd, cancellationToken);
    }

    public async Task<IReadOnlyList<GameCatalogItem>> GetCatalogInstalledAsync(CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(cancellationToken);
        await using var cmd = _connection!.CreateCommand();
        cmd.CommandText = """
            SELECT * FROM games
            WHERE is_catalog_title = 1 AND is_installed = 1
            ORDER BY tier ASC, title ASC
            """;
        return await ReadGamesAsync(cmd, cancellationToken);
    }

    public async Task<IReadOnlyList<GameCatalogItem>> GetAllGamesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(cancellationToken);
        await using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT * FROM games ORDER BY title ASC";
        return await ReadGamesAsync(cmd, cancellationToken);
    }

    public async Task<int> CountCatalogTitlesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(cancellationToken);
        await using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM games WHERE is_catalog_title = 1";
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task<int> CountGamesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(cancellationToken);
        await using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM games";
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task<HashSet<int>> GetInstalledSteamAppIdsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(cancellationToken);
        await using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT steam_app_id FROM games WHERE is_installed = 1";
        var ids = new HashSet<int>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            ids.Add(reader.GetInt32(0));
        }

        return ids;
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
                reader.GetInt32(12) == 1,
                reader.FieldCount > 14 && !reader.IsDBNull(14) && reader.GetInt32(14) == 1));
        }

        return items;
    }
}
