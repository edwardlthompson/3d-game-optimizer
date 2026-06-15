using SpatialLabsOptimizer.Infrastructure.Artwork;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Steam;

namespace SpatialLabsOptimizer.Infrastructure.Library;

public sealed partial class LibraryPrefetchService
{
    private readonly GameDatabase _database;
    private readonly GameArtworkService _artwork;
    private readonly OperationProgressHub _progressHub;
    private readonly PlatformConnectionRepository? _connections;
    private readonly SteamAppReviewsClient? _reviewsClient;
    private readonly PlayerCountService? _playerCounts;
    private readonly CoverArtCache _coverCache = new();

    public LibraryPrefetchService(
        GameDatabase database,
        GameArtworkService artwork,
        OperationProgressHub progressHub,
        PlatformConnectionRepository? connections = null,
        SteamAppReviewsClient? reviewsClient = null,
        PlayerCountService? playerCounts = null)
    {
        _database = database;
        _artwork = artwork;
        _progressHub = progressHub;
        _connections = connections;
        _reviewsClient = reviewsClient;
        _playerCounts = playerCounts;
    }

    public Task PrefetchArtworkAsync(IReadOnlyList<int> appIds) =>
        PrefetchArtworkInBackgroundAsync(appIds);

    public Task PrefetchMetadataAsync(IReadOnlyList<int> appIds) =>
        PrefetchMetadataInBackgroundAsync(appIds);

    public Task PrefetchMissingArtworkAsync(IReadOnlyList<int> appIds) =>
        PrefetchMissingArtworkInBackgroundAsync(appIds);

    private async Task PrefetchMissingArtworkInBackgroundAsync(IReadOnlyList<int> appIds)
    {
        var missing = new List<int>();
        foreach (var appId in appIds)
        {
            if (appId <= 0)
            {
                continue;
            }

            var game = await _database.GetGameAsync(appId);
            if (!string.IsNullOrWhiteSpace(game?.CoverCachePath) && File.Exists(game.CoverCachePath))
            {
                continue;
            }

            if (_coverCache.TryGetCached(appId, out _))
            {
                continue;
            }

            missing.Add(appId);
        }

        if (missing.Count == 0)
        {
            _progressHub.Publish(new OperationProgressReport(
                "artwork-prefetch",
                Application.Progress.OperationCategory.Index,
                "Fetching cover art",
                "All covers already cached",
                IsComplete: true,
                PercentComplete: 100));
            return;
        }

        await PrefetchArtworkInBackgroundAsync(missing);
    }

    private async Task PrefetchArtworkInBackgroundAsync(IReadOnlyList<int> appIds)
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "3d-game-optimizer", "logs");
        Directory.CreateDirectory(logDir);
        var logPath = Path.Combine(logDir, "artwork-prefetch.log");
        var errorCount = 0;

        for (var i = 0; i < appIds.Count; i++)
        {
            var appId = appIds[i];
            _progressHub.Publish(new OperationProgressReport(
                "artwork-prefetch",
                Application.Progress.OperationCategory.Index,
                "Fetching cover art",
                $"Steam AppID {appId}",
                StepIndex: i + 1,
                TotalSteps: appIds.Count,
                PercentComplete: (i + 1) * 100.0 / appIds.Count));

            try
            {
                var cover = await _artwork.ResolveCoverPathAsync(appId);
                if (!string.IsNullOrWhiteSpace(cover))
                {
                    var existing = await _database.GetGameAsync(appId);
                    if (existing is not null)
                    {
                        await _database.UpsertGameAsync(existing with { CoverCachePath = cover });
                    }
                }
            }
            catch (Exception ex)
            {
                errorCount++;
                await File.AppendAllTextAsync(
                    logPath,
                    $"[{DateTimeOffset.UtcNow:u}] AppID {appId}: {ex.Message}{Environment.NewLine}");
            }
        }

        _progressHub.Publish(new OperationProgressReport(
            "artwork-prefetch",
            Application.Progress.OperationCategory.Index,
            "Fetching cover art",
            errorCount == 0
                ? "Cover art prefetch complete"
                : $"Cover art prefetch complete ({errorCount} errors — see artwork-prefetch.log)",
            IsComplete: true,
            PercentComplete: 100));
    }
}
