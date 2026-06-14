using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Artwork;
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
    private readonly GameArtworkService _artwork;
    private readonly OperationProgressHub _progressHub;
    private readonly DisplayAutoDetector _displayDetector;

    public LibraryIndexer(
        CompatibilityRepository compatibility,
        SteamVdfScanner vdfScanner,
        LaunchReadinessService readiness,
        GameDatabase database,
        GameArtworkService artwork,
        OperationProgressHub progressHub,
        DisplayAutoDetector displayDetector)
    {
        _compatibility = compatibility;
        _vdfScanner = vdfScanner;
        _readiness = readiness;
        _database = database;
        _artwork = artwork;
        _progressHub = progressHub;
        _displayDetector = displayDetector;
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
            var cover = await _artwork.ResolveCoverPathAsync(entry.SteamAppId, cancellationToken);

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
                null,
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
    }
}

public sealed class PinnedShelfRepository
{
    private readonly SqliteSettingsStore _settings;

    public PinnedShelfRepository(SqliteSettingsStore settings)
    {
        _settings = settings;
    }

    public async Task<IReadOnlyList<int>> GetPinnedAppIdsAsync(CancellationToken cancellationToken = default)
    {
        var raw = await _settings.GetAsync("pinned_shelf", cancellationToken);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<int>();
        }

        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s, out var id) ? id : 0)
            .Where(id => id > 0)
            .ToList();
    }

    public async Task SetPinnedAppIdsAsync(IEnumerable<int> appIds, CancellationToken cancellationToken = default)
    {
        await _settings.SetAsync("pinned_shelf", string.Join(',', appIds), cancellationToken);
    }
}

public sealed class LocalPlaylistRepository
{
    private readonly SqliteSettingsStore _settings;

    public LocalPlaylistRepository(SqliteSettingsStore settings)
    {
        _settings = settings;
    }

    public Task SavePlaylistAsync(string name, IReadOnlyList<int> appIds, CancellationToken cancellationToken = default)
        => _settings.SetAsync($"playlist:{name}", string.Join(',', appIds), cancellationToken);
}
