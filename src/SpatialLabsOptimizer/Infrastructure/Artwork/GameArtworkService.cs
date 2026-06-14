using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Steam;

namespace SpatialLabsOptimizer.Infrastructure.Artwork;

public sealed class CoverArtCache
{
    private readonly string _cacheDir;

    public CoverArtCache(string? cacheDir = null)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _cacheDir = cacheDir ?? Path.Combine(appData, "3d-game-optimizer", "cache", "covers");
        Directory.CreateDirectory(_cacheDir);
    }

    public string GetCachePath(int appId) => Path.Combine(_cacheDir, $"{appId}.jpg");

    public bool TryGetCached(int appId, out string path)
    {
        path = GetCachePath(appId);
        return File.Exists(path);
    }
}

public sealed class GameArtworkService
{
    private readonly SteamStoreApiClient _storeClient;
    private readonly ExternalDataGateway _gateway;
    private readonly CoverArtCache _cache;
    private readonly OperationProgressHub _progressHub;

    public GameArtworkService(
        SteamStoreApiClient storeClient,
        ExternalDataGateway gateway,
        CoverArtCache cache,
        OperationProgressHub progressHub)
    {
        _storeClient = storeClient;
        _gateway = gateway;
        _cache = cache;
        _progressHub = progressHub;
    }

    public async Task<string?> ResolveCoverPathAsync(int appId, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetCached(appId, out var cachedPath))
        {
            return cachedPath;
        }

        var details = await _storeClient.GetAppDetailsAsync(appId, cancellationToken);
        var url = details?.CapsuleImage
            ?? details?.HeaderImage
            ?? $"https://steamcdn-a.akamaihd.net/steam/apps/{appId}/library_600x900.jpg";

        var bytes = await _gateway.GetBytesAsync(url, $"cover-{appId}", null, cancellationToken);
        if (bytes is null || bytes.Length == 0)
        {
            return null;
        }

        var path = _cache.GetCachePath(appId);
        await File.WriteAllBytesAsync(path, bytes, cancellationToken);

        _progressHub.Publish(new OperationProgressReport(
            $"cover-{appId}",
            Application.Progress.OperationCategory.Download,
            "Cover art cached",
            details?.Name ?? appId.ToString(),
            IsComplete: true));

        return path;
    }
}
