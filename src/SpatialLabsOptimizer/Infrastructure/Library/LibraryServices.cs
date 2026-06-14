using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Artwork;
using SpatialLabsOptimizer.Infrastructure.Compatibility;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Steam;
using SpatialLabsOptimizer.Infrastructure.Updates;

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
    private readonly EpicGogLibraryScanner? _externalScanner;
    private readonly LocalGameFolderRepository? _localFolders;
    private readonly LocalFolderGameScanner? _localScanner;
    private readonly LocalGameInstallResolver? _localInstallResolver;

    public LibraryIndexer(
        CompatibilityRepository compatibility,
        SteamVdfScanner vdfScanner,
        LaunchReadinessService readiness,
        GameDatabase database,
        GameArtworkService artwork,
        OperationProgressHub progressHub,
        DisplayAutoDetector displayDetector,
        EpicGogLibraryScanner? externalScanner = null,
        LocalGameFolderRepository? localFolders = null,
        LocalFolderGameScanner? localScanner = null,
        LocalGameInstallResolver? localInstallResolver = null)
    {
        _compatibility = compatibility;
        _vdfScanner = vdfScanner;
        _readiness = readiness;
        _database = database;
        _artwork = artwork;
        _progressHub = progressHub;
        _displayDetector = displayDetector;
        _externalScanner = externalScanner;
        _localFolders = localFolders;
        _localScanner = localScanner;
        _localInstallResolver = localInstallResolver;
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

        if (_externalScanner is not null)
        {
            await MergeExternalGamesAsync(_externalScanner.ScanEpicInstalledGames(), installed, adapter, cancellationToken);
            await MergeExternalGamesAsync(_externalScanner.ScanGogInstalledGames(), installed, adapter, cancellationToken);
        }

        if (_localFolders is not null && _localScanner is not null)
        {
            await MergeLocalGamesAsync(installed, adapter, cancellationToken);
        }
    }

    private async Task MergeLocalGamesAsync(
        HashSet<int> steamInstalled,
        IDisplayVendorAdapter? adapter,
        CancellationToken cancellationToken)
    {
        var folders = await _localFolders!.GetFoldersAsync(cancellationToken);
        var scanned = _localScanner!.ScanFolders(folders);
        var activeIds = new List<int>();

        foreach (var localGame in scanned)
        {
            activeIds.Add(localGame.StableAppId);
            await _database.UpsertLocalInstallAsync(
                localGame.StableAppId,
                localGame.FolderPath,
                localGame.LaunchExe,
                localGame.DisplayTitle,
                isStale: false,
                cancellationToken);

            if (steamInstalled.Contains(localGame.StableAppId))
            {
                continue;
            }

            var tier = CompatibilityTier.Experimental;
            var readiness = await _readiness.EvaluateAsync(localGame.StableAppId, true, tier, cancellationToken);
            await _database.UpsertGameAsync(new GameCatalogItem(
                localGame.StableAppId,
                localGame.DisplayTitle,
                tier,
                readiness,
                true,
                null,
                null,
                null,
                null,
                null,
                "Local",
                false), cancellationToken);
        }

        await _database.MarkLocalInstallsStaleExceptAsync(activeIds, cancellationToken);
        if (_localInstallResolver is not null)
        {
            await _localInstallResolver.WarmCacheAsync(cancellationToken);
        }
    }

    private async Task MergeExternalGamesAsync(
        IReadOnlyList<ExternalStoreGame> externalGames,
        HashSet<int> steamInstalled,
        IDisplayVendorAdapter? adapter,
        CancellationToken cancellationToken)
    {
        foreach (var externalGame in externalGames)
        {
            if (steamInstalled.Contains(externalGame.StableAppId))
            {
                continue;
            }

            var tier = CompatibilityTier.Experimental;
            var readiness = await _readiness.EvaluateAsync(externalGame.StableAppId, true, tier, cancellationToken);
            await _database.UpsertGameAsync(new GameCatalogItem(
                externalGame.StableAppId,
                $"{externalGame.Store}: {externalGame.Title}",
                tier,
                readiness,
                true,
                null,
                null,
                null,
                null,
                null,
                externalGame.Store,
                false), cancellationToken);
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

    public async Task RemovePinnedAppIdAsync(int appId, CancellationToken cancellationToken = default)
    {
        var pinned = (await GetPinnedAppIdsAsync(cancellationToken))
            .Where(id => id != appId)
            .ToList();
        await SetPinnedAppIdsAsync(pinned, cancellationToken);
    }

    public async Task<bool> IsPinnedAsync(int appId, CancellationToken cancellationToken = default)
    {
        var pinned = await GetPinnedAppIdsAsync(cancellationToken);
        return pinned.Contains(appId);
    }
}

public sealed class LocalPlaylistRepository
{
    private const string KeyPrefix = "playlist:";
    private readonly SqliteSettingsStore _settings;

    public LocalPlaylistRepository(SqliteSettingsStore settings)
    {
        _settings = settings;
    }

    public Task SavePlaylistAsync(string name, IReadOnlyList<int> appIds, CancellationToken cancellationToken = default)
        => _settings.SetAsync($"{KeyPrefix}{name}", string.Join(',', appIds), cancellationToken);

    public async Task<IReadOnlyList<int>> LoadPlaylistAsync(string name, CancellationToken cancellationToken = default)
    {
        var raw = await _settings.GetAsync($"{KeyPrefix}{name}", cancellationToken);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<int>();
        }

        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s, out var id) ? id : 0)
            .Where(id => id > 0)
            .ToList();
    }

    public async Task<IReadOnlyList<string>> ListPlaylistNamesAsync(CancellationToken cancellationToken = default)
    {
        var keys = await _settings.ListKeysByPrefixAsync(KeyPrefix, cancellationToken);
        return keys
            .Where(k => k.StartsWith(KeyPrefix, StringComparison.Ordinal))
            .Select(k => k[KeyPrefix.Length..])
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
