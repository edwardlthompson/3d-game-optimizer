using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Steam;

namespace SpatialLabsOptimizer.Infrastructure.Library;

public sealed partial class LibraryPrefetchService
{
    public Task PrefetchMissingMetadataAsync(IReadOnlyList<int> appIds) =>
        PrefetchMissingMetadataInBackgroundAsync(appIds);

    private async Task PrefetchMissingMetadataInBackgroundAsync(IReadOnlyList<int> appIds)
    {
        if (_reviewsClient is null)
        {
            return;
        }

        var missing = new List<int>();
        foreach (var appId in appIds)
        {
            if (appId <= 0)
            {
                continue;
            }

            var game = await _database.GetGameAsync(appId);
            if (game?.ReviewCount is > 0)
            {
                continue;
            }

            missing.Add(appId);
        }

        if (missing.Count == 0)
        {
            _progressHub.Publish(new OperationProgressReport(
                "metadata-prefetch",
                Application.Progress.OperationCategory.Index,
                "Fetching game metadata",
                "All metadata already cached",
                IsComplete: true,
                PercentComplete: 100));
            return;
        }

        await PrefetchMetadataInBackgroundAsync(missing);
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
