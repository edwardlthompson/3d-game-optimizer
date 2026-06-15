using Microsoft.Data.Sqlite;
using SpatialLabsOptimizer.Domain;

namespace SpatialLabsOptimizer.Infrastructure.Data;

public sealed partial class GameDatabase
{
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
                reader.GetInt32(12) == 1));
        }

        return items;
    }
}
