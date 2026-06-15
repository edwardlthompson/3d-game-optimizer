using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure;
using SpatialLabsOptimizer.Infrastructure.Compatibility;
using SpatialLabsOptimizer.Infrastructure.Library;
namespace SpatialLabsOptimizer.ViewModels;

public sealed partial class GameLibraryViewModel
{
    public bool IsLibraryLoaded { get; private set; }

    public async Task LoadAsync() => await LoadFromCacheAsync();

    public async Task RefreshLibraryAsync()
    {
        await _indexer.IndexAsync();
        await _indexer.MarkFullIndexCompletedAsync();
        if (_incrementalScan is not null)
        {
            await _incrementalScan.ScanNewGamesAsync(force: true);
        }

        await LoadFromCacheAsync();
    }

    public async Task RefreshCoverArtAsync()
    {
        await _database.InitializeAsync();
        var appIds = Games.Select(g => g.SteamAppId).Where(id => id > 0).Distinct().ToList();
        if (appIds.Count == 0)
        {
            var cached = await _database.GetCompatible3DAsync();
            appIds = cached.Select(g => g.SteamAppId).Where(id => id > 0).Distinct().ToList();
        }

        if (appIds.Count > 0)
        {
            await _prefetch.PrefetchMissingArtworkAsync(appIds);
        }

        await HydrateCoverTilesAsync();
    }

    public async Task LoadFromCacheAsync()
    {
        if (!_libraryPrefsLoaded)
        {
            await LoadLibraryPrefsAsync();
            _libraryPrefsLoaded = true;
        }

        if (_hdrWatchdog is not null && await _hdrWatchdog.IsHdrEnabledAsync())
        {
            await _hdrWatchdog.DisableHdrFor3DAsync();
        }

        await _database.InitializeAsync();
        var cached = ShowWhyNotReady
            ? await _database.GetCatalogInstalledAsync()
            : await _database.GetCompatible3DAsync();
        cached = ApplyFilters(cached);
        cached = await ApplyCatalogFiltersAsync(cached);

        var pinned = await _pinnedShelf.GetPinnedAppIdsAsync();
        WarmStartStatus = cached.Count > 0
            ? $"3D catalog — {cached.Count} title(s) ({pinned.Count} pinned, queue {_playQueue.Count})"
            : ShowWhyNotReady
                ? "No installed 3D catalog titles — use Refresh library after connecting Steam."
                : ShowLocalOnly
                    ? "Add a folder in Library Settings to find local installs."
                    : "No 3D-ready installs — run Refresh library or check compatibility seed.";

        Games = await MapAndSortAsync(cached, pinned);
        PlaylistNames = await _playlists.ListPlaylistNamesAsync();
        RecentLaunches = (await _intelligence.GetRecentLaunchesAsync())
            .Select(e => new RecentLaunchItemViewModel(e))
            .ToList();

        if (SelectedGame is not null)
        {
            await RefreshSelectedGameDetailsAsync();
        }

        await HydrateCoverTilesAsync();
        IsLibraryLoaded = true;
        LibraryUpdated?.Invoke(this, EventArgs.Empty);
        V2Enabled = FeatureFlags.V2Enabled;
    }

    private async Task RefreshSelectedGameDetailsAsync()
    {
        if (SelectedGame is null)
        {
            return;
        }

        var item = SelectedGame;
        var existing = await _overrides.GetAsync(item.SteamAppId);
        PreferredOutput = existing?.PreferredOutput ?? "Auto";
        CompatibilityNote = await _intelligence.GetCompatibilityNoteAsync(item.SteamAppId) ?? "";
        SelectedPresetFreshness = await _intelligence.GetPresetFreshnessLabelAsync(item.SteamAppId);
        var catalog = await _compatibility.GetCatalogMetadataByAppIdAsync(item.SteamAppId);
        SelectedRecommendedTools = CatalogToolHints.DescribeRecommendedStack(catalog);
        WhyNotReadyHint = BuildWhyNotReadyHint(item.Source);
    }

    private IReadOnlyList<GameCatalogItem> ApplyFilters(IReadOnlyList<GameCatalogItem> items)
    {
        var filtered = items.AsEnumerable();
        if (ShowFavoritesOnly)
        {
            filtered = filtered.Where(g => g.IsFavorite);
        }

        if (ShowLocalOnly)
        {
            filtered = filtered.Where(g => string.Equals(g.ReviewDescriptor, "Local", StringComparison.OrdinalIgnoreCase));
        }

        if (ShowWhyNotReady)
        {
            filtered = _intelligence.ApplyWhyNotReadyFilter(filtered.ToList());
        }

        if (SmartCollection != SmartCollectionMode.None)
        {
            filtered = _intelligence.ApplySmartCollection(filtered.ToList(), SmartCollection);
        }

        return filtered.ToList();
    }

    private static string BuildWhyNotReadyHint(GameCatalogItem item) => item.Readiness switch
    {
        LaunchReadinessState.NeedsInstall => "Install the game or add its folder under Library Settings.",
        LaunchReadinessState.NeedsPresetCache => "Cache a preset using Refresh preset or run setup bulk cache.",
        LaunchReadinessState.NeedsToolchain => "Open Settings → Toolchain & display to install required 3D tools.",
        LaunchReadinessState.Blocked => "Compatibility tier blocks launch — review notes or try Safe launch.",
        _ => "Ready to play in 3D."
    };

    private void ApplySort()
    {
        if (Games.Count == 0)
        {
            return;
        }

        var pinned = Games.Where(g => g.IsPinned).Select(g => g.SteamAppId).ToHashSet();
        var sorted = _sortService.Sort(Games.Select(g => g.Source).ToList(), SortMode);
        Games = sorted.Select(g => new GameLibraryItemViewModel(
            g,
            pinned.Contains(g.SteamAppId),
            LibraryIntelligenceService.GetCompatibilityBadge(
                g.Tier,
                g.Readiness,
                string.Equals(g.ReviewDescriptor, "Local", StringComparison.OrdinalIgnoreCase)),
            null,
            null)).ToList();
        LibraryUpdated?.Invoke(this, EventArgs.Empty);
    }

    private async Task<IReadOnlyList<GameLibraryItemViewModel>> MapAndSortAsync(
        IReadOnlyList<GameCatalogItem> items,
        IReadOnlyList<int> pinnedIds)
    {
        var pinned = pinnedIds.ToHashSet();
        var sorted = _sortService.Sort(items, SortMode);
        var viewModels = new List<GameLibraryItemViewModel>();
        foreach (var g in sorted)
        {
            var freshness = await _intelligence.GetPresetFreshnessLabelAsync(g.SteamAppId);
            var catalog = await _compatibility.GetCatalogMetadataByAppIdAsync(g.SteamAppId);
            var sourceBadges = CatalogFilterHelper.BuildSourceBadges(catalog);
            viewModels.Add(new GameLibraryItemViewModel(
                g,
                pinned.Contains(g.SteamAppId),
                LibraryIntelligenceService.GetCompatibilityBadge(
                    g.Tier,
                    g.Readiness,
                    string.Equals(g.ReviewDescriptor, "Local", StringComparison.OrdinalIgnoreCase)),
                freshness,
                sourceBadges));
        }

        return viewModels;
    }
}
