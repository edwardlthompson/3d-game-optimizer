using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Steam;
using SpatialLabsOptimizer.Infrastructure.Updates;

namespace SpatialLabsOptimizer.Infrastructure.Artwork;

public sealed partial class GameArtworkService
{
    private static readonly string[] CdnUrlTemplates =
    [
        "https://steamcdn-a.akamaihd.net/steam/apps/{0}/library_600x900.jpg",
        "https://steamcdn-a.akamaihd.net/steam/apps/{0}/library_600x900_2x.jpg",
        "https://steamcdn-a.akamaihd.net/steam/apps/{0}/header.jpg",
        "https://steamcdn-a.akamaihd.net/steam/apps/{0}/capsule_616x353.jpg"
    ];

    private readonly SteamStoreApiClient _storeClient;
    private readonly ExternalDataGateway _gateway;
    private readonly CoverArtCache _cache;
    private readonly OperationProgressHub _progressHub;
    private readonly SteamGridDbClient? _steamGridDb;

    public GameArtworkService(
        SteamStoreApiClient storeClient,
        ExternalDataGateway gateway,
        CoverArtCache cache,
        OperationProgressHub progressHub,
        SteamGridDbClient? steamGridDb = null)
    {
        _storeClient = storeClient;
        _gateway = gateway;
        _cache = cache;
        _progressHub = progressHub;
        _steamGridDb = steamGridDb;
    }

    public async Task<string?> ResolveCoverPathAsync(int appId, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetCached(appId, out var cachedPath))
        {
            return cachedPath;
        }

        var path = _cache.GetCachePath(appId);
        foreach (var url in BuildCdnUrls(appId))
        {
            var bytes = await TryDownloadImageAsync(url, $"cover-cdn-{appId}", cancellationToken);
            if (bytes is not null)
            {
                return await WriteCoverAsync(path, bytes, appId, "Steam CDN", cancellationToken);
            }
        }

        var details = await _storeClient.GetAppDetailsAsync(appId, cancellationToken);
        foreach (var url in BuildStoreUrls(appId, details))
        {
            var bytes = await TryDownloadImageAsync(url, $"cover-store-{appId}", cancellationToken);
            if (bytes is not null)
            {
                return await WriteCoverAsync(path, bytes, appId, details?.Name ?? appId.ToString(), cancellationToken);
            }
        }

        return await TrySteamGridDbFallbackAsync(appId, cancellationToken);
    }

    private static IEnumerable<string> BuildCdnUrls(int appId) =>
        CdnUrlTemplates.Select(template => string.Format(template, appId));

    private static IEnumerable<string> BuildStoreUrls(int appId, SteamAppDetails? details)
    {
        if (!string.IsNullOrWhiteSpace(details?.CapsuleImage))
        {
            yield return details.CapsuleImage;
        }

        if (!string.IsNullOrWhiteSpace(details?.HeaderImage))
        {
            yield return details.HeaderImage!;
        }
    }

    private async Task<byte[]?> TryDownloadImageAsync(
        string url,
        string operationId,
        CancellationToken cancellationToken)
    {
        var (bytes, _) = await _gateway.TryGetBytesAsync(url, operationId, cancellationToken: cancellationToken);
        return bytes is { Length: > 0 } && IsValidImageBytes(bytes) ? bytes : null;
    }

    private static bool IsValidImageBytes(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
        {
            return true;
        }

        return bytes.Length >= 8
            && bytes[0] == 0x89
            && bytes[1] == 0x50
            && bytes[2] == 0x4E
            && bytes[3] == 0x47;
    }

    private async Task<string> WriteCoverAsync(
        string path,
        byte[] bytes,
        int appId,
        string label,
        CancellationToken cancellationToken)
    {
        await File.WriteAllBytesAsync(path, bytes, cancellationToken);
        _progressHub.Publish(new OperationProgressReport(
            $"cover-{appId}",
            Application.Progress.OperationCategory.Download,
            "Cover art cached",
            label,
            IsComplete: true));
        return path;
    }
}
