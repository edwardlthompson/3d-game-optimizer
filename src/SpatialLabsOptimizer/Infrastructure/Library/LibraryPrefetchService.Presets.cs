using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Artwork;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Progress;

namespace SpatialLabsOptimizer.Infrastructure.Library;

public sealed partial class LibraryPrefetchService
{
    public Task PrefetchMissingPresetsAsync(IReadOnlyList<int> appIds) =>
        PrefetchMissingPresetsInBackgroundAsync(appIds);

    private async Task PrefetchMissingPresetsInBackgroundAsync(IReadOnlyList<int> appIds)
    {
        if (_presets is null)
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
            if (!SteamCoverArtPolicy.IsEligible(game))
            {
                continue;
            }

            if (await _presets.HasPresetAsync(appId))
            {
                continue;
            }

            missing.Add(appId);
        }

        if (missing.Count == 0)
        {
            _progressHub.Publish(new OperationProgressReport(
                "preset-prefetch",
                Application.Progress.OperationCategory.Download,
                "Caching presets",
                "All presets already cached",
                IsComplete: true,
                PercentComplete: 100));
            return;
        }

        await PrefetchPresetsInBackgroundAsync(missing);
    }

    private async Task PrefetchPresetsInBackgroundAsync(IReadOnlyList<int> appIds)
    {
        if (_presets is null)
        {
            return;
        }

        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "3d-game-optimizer", "logs");
        Directory.CreateDirectory(logDir);
        var logPath = Path.Combine(logDir, "preset-prefetch.log");
        var errorCount = 0;

        for (var i = 0; i < appIds.Count; i++)
        {
            var appId = appIds[i];
            _progressHub.Publish(new OperationProgressReport(
                "preset-prefetch",
                Application.Progress.OperationCategory.Download,
                "Caching presets",
                $"Steam AppID {appId}",
                StepIndex: i + 1,
                TotalSteps: appIds.Count,
                PercentComplete: (i + 1) * 100.0 / appIds.Count));

            try
            {
                await _presets.CachePresetAsync(appId);
                await RefreshReadinessAfterPresetAsync(appId);
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
            "preset-prefetch",
            Application.Progress.OperationCategory.Download,
            "Caching presets",
            errorCount == 0
                ? "Preset prefetch complete"
                : $"Preset prefetch complete ({errorCount} errors — see preset-prefetch.log)",
            IsComplete: true,
            PercentComplete: 100));
    }

    private async Task RefreshReadinessAfterPresetAsync(int appId)
    {
        if (_readiness is null)
        {
            return;
        }

        var game = await _database.GetGameAsync(appId);
        if (game is null || game.Readiness == LaunchReadinessState.Ready)
        {
            return;
        }

        var readiness = await _readiness.EvaluateAsync(appId, game.IsInstalled, game.Tier);
        if (readiness == game.Readiness)
        {
            return;
        }

        await _database.UpsertGameAsync(game with { Readiness = readiness });
    }
}
