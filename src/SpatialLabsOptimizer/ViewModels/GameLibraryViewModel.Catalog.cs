using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Compatibility;
using SpatialLabsOptimizer.Infrastructure.Updates;
using Windows.System;

namespace SpatialLabsOptimizer.ViewModels;

public sealed partial class GameLibraryViewModel
{
    public const string CatalogSiteUrl = "https://edwardlthompson.github.io/3d-game-optimizer/catalog/";

    public ICommand OpenCatalogCommand { get; }

    public bool FilterUltraNative
    {
        get => _filterUltraNative;
        set
        {
            if (SetProperty(ref _filterUltraNative, value))
            {
                _ = LoadAsync();
                ScheduleSaveLibraryPrefs();
            }
        }
    }

    public bool FilterTrueGame
    {
        get => _filterTrueGame;
        set
        {
            if (SetProperty(ref _filterTrueGame, value))
            {
                _ = LoadAsync();
                ScheduleSaveLibraryPrefs();
            }
        }
    }

    public bool FilterUevr
    {
        get => _filterUevr;
        set
        {
            if (SetProperty(ref _filterUevr, value))
            {
                _ = LoadAsync();
                ScheduleSaveLibraryPrefs();
            }
        }
    }

    public bool Filter3DVision
    {
        get => _filter3DVision;
        set
        {
            if (SetProperty(ref _filter3DVision, value))
            {
                _ = LoadAsync();
                ScheduleSaveLibraryPrefs();
            }
        }
    }

    public async Task OpenCatalogSiteAsync()
    {
        await Launcher.LaunchUriAsync(new Uri(CatalogSiteUrl));
    }

    private async Task<IReadOnlyList<GameCatalogItem>> ApplyCatalogFiltersAsync(
        IReadOnlyList<GameCatalogItem> items)
    {
        if (!FilterUltraNative && !FilterTrueGame && !FilterUevr && !Filter3DVision)
        {
            return items;
        }

        var filtered = new List<GameCatalogItem>();
        foreach (var item in items)
        {
            var catalog = await _compatibility.GetCatalogMetadataByAppIdAsync(item.SteamAppId);
            if (FilterUltraNative && !CatalogFilterHelper.MatchesUltraNative(catalog))
            {
                continue;
            }

            if (FilterTrueGame && !CatalogFilterHelper.MatchesTrueGame(catalog))
            {
                continue;
            }

            if (FilterUevr && !CatalogFilterHelper.MatchesUevr(catalog, await GetVrCapabilityAsync(item.SteamAppId)))
            {
                continue;
            }

            if (Filter3DVision && !CatalogFilterHelper.Matches3DVision(catalog))
            {
                continue;
            }

            filtered.Add(item);
        }

        return filtered;
    }

    private async Task<VrCapability> GetVrCapabilityAsync(int appId)
    {
        var entry = await _compatibility.GetByAppIdAsync(appId);
        return entry?.VrCapability ?? VrCapability.None;
    }
}
