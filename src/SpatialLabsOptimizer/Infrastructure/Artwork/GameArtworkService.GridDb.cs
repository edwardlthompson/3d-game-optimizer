using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Updates;

namespace SpatialLabsOptimizer.Infrastructure.Artwork;

public sealed partial class GameArtworkService
{
    private async Task<string?> TrySteamGridDbFallbackAsync(int appId, CancellationToken cancellationToken)
    {
        if (_steamGridDb is null)
        {
            return null;
        }

        var fallback = await _steamGridDb.ResolveCoverAsync(appId, cancellationToken);
        if (string.IsNullOrWhiteSpace(fallback))
        {
            return null;
        }

        if (File.Exists(fallback))
        {
            return fallback;
        }

        if (!fallback.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !fallback.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var bytes = await TryDownloadImageAsync(fallback, $"cover-grid-{appId}", cancellationToken);
        if (bytes is null)
        {
            return null;
        }

        var path = _cache.GetCachePath(appId);
        return await WriteCoverAsync(path, bytes, appId, $"SteamGridDB {appId}", cancellationToken);
    }
}
