using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Compatibility;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Steam;

namespace SpatialLabsOptimizer.Infrastructure.Library;

public sealed class LibraryIndexer
{
    private readonly CompatibilityRepository _compatibility;
    private readonly SteamVdfScanner _vdfScanner;
    private readonly LaunchReadinessService _readiness;
    private readonly GameDatabase _database;
    private readonly OperationProgressHub _progressHub;
    private readonly DisplayAutoDetector _displayDetector;
    private readonly LibraryIndexMerger _merger;
    private readonly LibraryPrefetchService _prefetch;

    public LibraryIndexer(
        CompatibilityRepository compatibility,
        SteamVdfScanner vdfScanner,
        LaunchReadinessService readiness,
        GameDatabase database,
        OperationProgressHub progressHub,
        DisplayAutoDetector displayDetector,
        LibraryIndexMerger merger,
        LibraryPrefetchService prefetch)
    {
        _compatibility = compatibility;
        _vdfScanner = vdfScanner;
        _readiness = readiness;
        _database = database;
        _progressHub = progressHub;
        _displayDetector = displayDetector;
        _merger = merger;
        _prefetch = prefetch;
    }

    public async Task IndexAsync(CancellationToken cancellationToken = default)
    {
        var installed = _vdfScanner.ScanInstalledAppIds().ToHashSet();
        var entries = await _compatibility.GetAllAsync(cancellationToken);
        var profile = await _displayDetector.DetectAsync(cancellationToken);
        var adapter = profile is not null ? _displayDetector.CreateAdapter(profile) : null;

        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var tier = adapter is not null
                ? _compatibility.GetTierForVendor(entry, adapter.Vendor)
                : CompatibilityTier.Experimental;
            var isInstalled = installed.Contains(entry.SteamAppId);
            var readiness = await _readiness.EvaluateAsync(entry.SteamAppId, isInstalled, tier, cancellationToken);
            var existing = await _database.GetGameAsync(entry.SteamAppId, cancellationToken);
            var cover = existing?.CoverCachePath;

            await _database.UpsertGameAsync(new GameCatalogItem(
                entry.SteamAppId,
                entry.Title,
                tier,
                readiness,
                isInstalled,
                entry.CurrentPlayers,
                entry.ReviewScorePercent,
                entry.ReviewCount,
                entry.ReviewSortScore,
                cover,
                entry.SteamTags.FirstOrDefault(),
                false), cancellationToken);

            _progressHub.Publish(new OperationProgressReport(
                "library-index",
                Application.Progress.OperationCategory.Index,
                "Indexing library",
                entry.Title,
                StepIndex: i + 1,
                TotalSteps: entries.Count,
                PercentComplete: (i + 1) * 100.0 / entries.Count));
        }

        await _merger.MergeExternalStoresAsync(installed, adapter, cancellationToken);

        _progressHub.Publish(new OperationProgressReport(
            "library-index",
            Application.Progress.OperationCategory.Index,
            "Indexing library",
            "Library index complete",
            IsComplete: true,
            PercentComplete: 100));

        var allGames = await _database.GetAllGamesAsync(cancellationToken);
        var steamAppIds = allGames.Where(g => g.SteamAppId > 0).Select(g => g.SteamAppId).Distinct().ToList();
        _ = _prefetch.PrefetchArtworkAsync(steamAppIds);
        _ = _prefetch.PrefetchMetadataAsync(steamAppIds);
    }
}
