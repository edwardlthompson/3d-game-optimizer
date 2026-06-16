using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Data;

namespace SpatialLabsOptimizer.Infrastructure.Artwork;

public static class CoverArtCacheSync
{
    public static async Task SyncMissingPathsAsync(
        GameDatabase database,
        CoverArtCache cache,
        CancellationToken cancellationToken = default)
    {
        var games = await database.GetAllGamesAsync(cancellationToken);
        foreach (var game in games)
        {
            if (game.SteamAppId <= 0 || !SteamCoverArtPolicy.IsEligible(game))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(game.CoverCachePath) && File.Exists(game.CoverCachePath))
            {
                continue;
            }

            if (!cache.TryGetCached(game.SteamAppId, out var path))
            {
                continue;
            }

            await database.UpsertGameAsync(game with { CoverCachePath = path }, cancellationToken);
        }
    }
}
