using SpatialLabsOptimizer.Infrastructure.Artwork;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Steam;

namespace SpatialLabsOptimizer.Infrastructure.Library;

public sealed class LibraryPrefetchService
{
    private readonly GameDatabase _database;
    private readonly GameArtworkService _artwork;
    private readonly OperationProgressHub _progressHub;
    private readonly PlatformConnectionRepository? _connections;
    private readonly SteamAppReviewsClient? _reviewsClient;
    private readonly PlayerCountService? _playerCounts;

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

    private async Task PrefetchMetadataInBackgroundAsync(IReadOnlyList<int> appIds)
    {
        if (_reviewsClient is null)
        {
            return;
        }

        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "3d-game-optimizer", "logs");
        Directory.CreateDirectory(logDir);
        var logPath = Path.Combine(logDir, "metadata-prefetch.log");
        var apiKey = _connections is not null ? await _connections.GetSteamApiKeyAsync() : null;

        for (var i = 0; i < appIds.Count; i++)
        {
            var appId = appIds[i];
            _progressHub.Publish(new OperationProgressReport(
                "metadata-prefetch",
                Application.Progress.OperationCategory.Index,
                "Fetching game metadata",
                $"Steam AppID {appId}",
                StepIndex: i + 1,
                TotalSteps: appIds.Count,
                PercentComplete: (i + 1) * 100.0 / appIds.Count));

            try
            {
                var (percent, count, sortScore, descriptor) = await _reviewsClient.GetReviewSummaryAsync(appId);
                int? players = null;
                if (_playerCounts is not null && !string.IsNullOrWhiteSpace(apiKey))
                {
                    players = await _playerCounts.GetCurrentPlayersAsync(appId, apiKey);
                }

                var existing = await _database.GetGameAsync(appId);
                if (existing is not null && count > 0)
                {
                    await _database.UpsertGameAsync(existing with
                    {
                        ReviewScorePercent = percent,
                        ReviewCount = count,
                        ReviewSortScore = sortScore,
                        ReviewDescriptor = descriptor ?? existing.ReviewDescriptor,
                        CurrentPlayers = players ?? existing.CurrentPlayers
                    });
                }
            }
            catch (Exception ex)
            {
                await File.AppendAllTextAsync(
                    logPath,
                    $"[{DateTimeOffset.UtcNow:u}] AppID {appId}: {ex.Message}{Environment.NewLine}");
            }
        }

        _progressHub.Publish(new OperationProgressReport(
            "metadata-prefetch",
            Application.Progress.OperationCategory.Index,
            "Fetching game metadata",
            "Metadata prefetch complete",
            IsComplete: true,
            PercentComplete: 100));
    }
}
