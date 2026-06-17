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
                review_descriptor, cover_cache_path, is_favorite, is_catalog_title)
            VALUES ($id, $title, $tier, $readiness, $installed,
                $players, $reviewPct, $reviewCount, $reviewSort,
                $descriptor, $cover, $favorite, $catalog)
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
                is_catalog_title = CASE WHEN excluded.is_catalog_title = 1 THEN 1 ELSE games.is_catalog_title END
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
        cmd.Parameters.AddWithValue("$catalog", item.IsCatalogTitle ? 1 : 0);
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
}
