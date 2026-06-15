using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure;
using SpatialLabsOptimizer.Infrastructure.Library;
namespace SpatialLabsOptimizer.ViewModels;

public sealed partial class GameLibraryViewModel
{
    public async Task LoadAsync()
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

        if (_incrementalScan is not null)
        {
            await _incrementalScan.ScanNewGamesAsync();
        }

        await _database.InitializeAsync();
        var cached = ShowWhyNotReady
            ? await _database.GetAllGamesAsync()
            : await _database.GetReadyToPlayAsync();
        cached = ApplyFilters(cached);

        var pinned = await _pinnedShelf.GetPinnedAppIdsAsync();
        WarmStartStatus = cached.Count > 0
            ? $"Warm start — {cached.Count} titles ({pinned.Count} pinned, queue {_playQueue.Count})"
            : ShowWhyNotReady
                ? "No blocked titles in ready shelf — check full library index."
                : ShowLocalOnly
                    ? "Add a folder in Library Settings to find local installs."
                    : "Building library index…";

        Games = await MapAndSortAsync(cached, pinned);
        PlaylistNames = await _playlists.ListPlaylistNamesAsync();
        RecentLaunches = (await _intelligence.GetRecentLaunchesAsync())
            .Select(e => new RecentLaunchItemViewModel(e))
            .ToList();

        if (SelectedGame is not null)
        {
            var existing = await _overrides.GetAsync(SelectedGame.SteamAppId);
            PreferredOutput = existing?.PreferredOutput ?? "Auto";
            CompatibilityNote = await _intelligence.GetCompatibilityNoteAsync(SelectedGame.SteamAppId) ?? "";
            SelectedPresetFreshness = await _intelligence.GetPresetFreshnessLabelAsync(SelectedGame.SteamAppId);
            WhyNotReadyHint = BuildWhyNotReadyHint(SelectedGame.Source);
        }

        LibraryUpdated?.Invoke(this, EventArgs.Empty);
        V2Enabled = FeatureFlags.V2Enabled;
    }

    public async Task SelectGameAsync(GameLibraryItemViewModel item)
    {
        SelectedGame = item;
        var existing = await _overrides.GetAsync(item.SteamAppId);
        PreferredOutput = existing?.PreferredOutput ?? "Auto";
        CompatibilityNote = await _intelligence.GetCompatibilityNoteAsync(item.SteamAppId) ?? "";
        SelectedPresetFreshness = await _intelligence.GetPresetFreshnessLabelAsync(item.SteamAppId);
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
        LaunchReadinessState.NeedsToolchain => "Complete the setup wizard to install required 3D tools.",
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
            viewModels.Add(new GameLibraryItemViewModel(
                g,
                pinned.Contains(g.SteamAppId),
                LibraryIntelligenceService.GetCompatibilityBadge(
                    g.Tier,
                    g.Readiness,
                    string.Equals(g.ReviewDescriptor, "Local", StringComparison.OrdinalIgnoreCase)),
                freshness));
        }

        return viewModels;
    }
}
