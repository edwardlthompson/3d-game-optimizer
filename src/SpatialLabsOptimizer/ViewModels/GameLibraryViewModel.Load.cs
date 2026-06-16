using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure;
using SpatialLabsOptimizer.Infrastructure.Artwork;
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
        var appIds = Games
            .Where(g => SteamCoverArtPolicy.IsEligible(g.Source))
            .Select(g => g.SteamAppId)
            .Where(id => id > 0)
            .Distinct()
            .ToList();
        if (appIds.Count == 0)
        {
            var cached = await _database.GetCompatible3DAsync();
            appIds = cached
                .Where(SteamCoverArtPolicy.IsEligible)
                .Select(g => g.SteamAppId)
                .Where(id => id > 0)
                .Distinct()
                .ToList();
        }

        if (appIds.Count > 0)
        {
            await _prefetch.PrefetchArtworkAsync(appIds);
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
        await CoverArtCacheSync.SyncMissingPathsAsync(_database, _coverCache);
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
        SelectedRank3DDisplay = catalog is { Rank3DScore: > 0 }
            ? $"3D Rank {catalog.Rank3DScore} — {catalog.Rank3DLabel}"
            : "3D Rank —";
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
        LaunchReadinessState.NeedsPresetCache => "Preset download in progress — refresh library if this persists.",
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

        var existing = Games.ToDictionary(g => g.SteamAppId);
        var pinned = Games.Where(g => g.IsPinned).Select(g => g.SteamAppId).ToHashSet();
        var sources = Games.Select(g => g.Source).ToList();
        var sorted = SortMode == LibrarySortMode.GameRank
            ? _sortService.SortByGameRank(
                sources,
                existing.ToDictionary(kv => kv.Key, kv => kv.Value.GameRankScore ?? double.NegativeInfinity),
                existing.ToDictionary(kv => kv.Key, kv => kv.Value.Rank3DScore))
            : _sortService.Sort(sources, SortMode);
        Games = sorted.Select(g => CreateLibraryItemViewModel(g, existing.GetValueOrDefault(g.SteamAppId), pinned)).ToList();
        LibraryUpdated?.Invoke(this, EventArgs.Empty);
    }

    private static GameLibraryItemViewModel CreateLibraryItemViewModel(
        GameCatalogItem item,
        GameLibraryItemViewModel? previous,
        IReadOnlySet<int> pinned)
    {
        var isLocal = string.Equals(item.ReviewDescriptor, "Local", StringComparison.OrdinalIgnoreCase);
        return new GameLibraryItemViewModel(
            item,
            pinned.Contains(item.SteamAppId),
            previous?.CompatibilityBadge ?? LibraryIntelligenceService.GetCompatibilityBadge(item.Tier, item.Readiness, isLocal),
            previous?.PresetFreshness,
            previous?.SourceBadges,
            previous?.Rank3DScore ?? 0,
            previous?.Rank3DLabel,
            previous?.GameRankScore);
    }

    private static double? ComputeGameRankScore(GameCatalogItem item, int rank3DScore) =>
        CatalogGameRankScorer.ScoreFromCatalogItem(
            item.ReviewScorePercent,
            item.ReviewCount,
            item.CurrentPlayers,
            rank3DScore);

    private async Task<IReadOnlyList<GameLibraryItemViewModel>> MapAndSortAsync(
        IReadOnlyList<GameCatalogItem> items,
        IReadOnlyList<int> pinnedIds)
    {
        var pinned = pinnedIds.ToHashSet();
        var sorted = await SortCatalogItemsAsync(items);
        var viewModels = new List<GameLibraryItemViewModel>();
        foreach (var g in sorted)
        {
            var freshness = await _intelligence.GetPresetFreshnessLabelAsync(g.SteamAppId);
            var catalog = await _compatibility.GetCatalogMetadataByAppIdAsync(g.SteamAppId);
            var sourceBadges = CatalogFilterHelper.BuildSourceBadges(catalog);
            var rank3DScore = catalog?.Rank3DScore ?? 0;
            viewModels.Add(new GameLibraryItemViewModel(
                g,
                pinned.Contains(g.SteamAppId),
                LibraryIntelligenceService.GetCompatibilityBadge(
                    g.Tier,
                    g.Readiness,
                    string.Equals(g.ReviewDescriptor, "Local", StringComparison.OrdinalIgnoreCase)),
                freshness,
                sourceBadges,
                rank3DScore,
                catalog?.Rank3DLabel,
                ComputeGameRankScore(g, rank3DScore)));
        }

        return viewModels;
    }

    private async Task<IReadOnlyList<GameCatalogItem>> SortCatalogItemsAsync(IReadOnlyList<GameCatalogItem> items)
    {
        if (SortMode != LibrarySortMode.GameRank)
        {
            return _sortService.Sort(items, SortMode);
        }

        var gameRankScores = new Dictionary<int, double>();
        var rank3DScores = new Dictionary<int, int>();
        foreach (var item in items)
        {
            var catalog = await _compatibility.GetCatalogMetadataByAppIdAsync(item.SteamAppId);
            var rank3DScore = catalog?.Rank3DScore ?? 0;
            rank3DScores[item.SteamAppId] = rank3DScore;
            gameRankScores[item.SteamAppId] = ComputeGameRankScore(item, rank3DScore)
                ?? double.NegativeInfinity;
        }

        return _sortService.SortByGameRank(items, gameRankScores, rank3DScores);
    }
}
