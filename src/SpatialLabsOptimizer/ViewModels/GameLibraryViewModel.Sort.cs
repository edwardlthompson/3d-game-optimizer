using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Compatibility;
using SpatialLabsOptimizer.Infrastructure.Library;

namespace SpatialLabsOptimizer.ViewModels;

public sealed partial class GameLibraryViewModel
{
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

    private sealed record CatalogRankContext(int Rank3DScore, string? Rank3DLabel, string SourceBadges, double? GameRankScore);

    private async Task<IReadOnlyDictionary<int, CatalogRankContext>> BuildCatalogRankContextsAsync(
        IReadOnlyList<GameCatalogItem> items)
    {
        var contexts = new Dictionary<int, CatalogRankContext>();
        foreach (var item in items)
        {
            var catalog = await _compatibility.GetCatalogMetadataByAppIdAsync(item.SteamAppId);
            var rank3DScore = catalog?.Rank3DScore ?? 0;
            contexts[item.SteamAppId] = new CatalogRankContext(
                rank3DScore,
                catalog?.Rank3DLabel,
                CatalogFilterHelper.BuildSourceBadges(catalog),
                ComputeGameRankScore(item, rank3DScore));
        }

        return contexts;
    }

    private async Task<IReadOnlyList<GameLibraryItemViewModel>> MapAndSortAsync(
        IReadOnlyList<GameCatalogItem> items,
        IReadOnlyList<int> pinnedIds)
    {
        var pinned = pinnedIds.ToHashSet();
        var rankContexts = await BuildCatalogRankContextsAsync(items);
        var sorted = SortMode == LibrarySortMode.GameRank
            ? _sortService.SortByGameRank(
                items,
                rankContexts.ToDictionary(kv => kv.Key, kv => kv.Value.GameRankScore ?? double.NegativeInfinity),
                rankContexts.ToDictionary(kv => kv.Key, kv => kv.Value.Rank3DScore))
            : _sortService.Sort(items, SortMode);

        var viewModels = new List<GameLibraryItemViewModel>();
        foreach (var g in sorted)
        {
            var ctx = rankContexts[g.SteamAppId];
            var freshness = await _intelligence.GetPresetFreshnessLabelAsync(g.SteamAppId);
            viewModels.Add(new GameLibraryItemViewModel(
                g,
                pinned.Contains(g.SteamAppId),
                LibraryIntelligenceService.GetCompatibilityBadge(
                    g.Tier,
                    g.Readiness,
                    string.Equals(g.ReviewDescriptor, "Local", StringComparison.OrdinalIgnoreCase)),
                freshness,
                ctx.SourceBadges,
                ctx.Rank3DScore,
                ctx.Rank3DLabel,
                ctx.GameRankScore));
        }

        return viewModels;
    }
}
