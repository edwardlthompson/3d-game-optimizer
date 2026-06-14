using SpatialLabsOptimizer.Domain;

namespace SpatialLabsOptimizer.Infrastructure.Library;

public enum LibrarySortMode
{
    Quality,
    PlayersOnline,
    SteamReviews,
    Name
}

public sealed class LibrarySortService
{
    public IReadOnlyList<GameCatalogItem> Sort(IReadOnlyList<GameCatalogItem> games, LibrarySortMode mode) => mode switch
    {
        LibrarySortMode.PlayersOnline => games.OrderByDescending(g => g.CurrentPlayers ?? 0).ThenBy(g => g.Title).ToList(),
        LibrarySortMode.SteamReviews => games.OrderByDescending(g => g.ReviewSortScore ?? 0).ThenByDescending(g => g.ReviewCount ?? 0).ToList(),
        LibrarySortMode.Name => games.OrderBy(g => g.Title).ToList(),
        _ => games.OrderBy(g => (int)g.Tier).ThenByDescending(g => g.ReviewSortScore ?? 0).ToList()
    };
}

public sealed class GameLibraryItemViewModel
{
    public GameLibraryItemViewModel(GameCatalogItem item)
    {
        SteamAppId = item.SteamAppId;
        Title = item.Title;
        Tier = item.Tier;
        Readiness = item.Readiness;
        CoverPath = item.CoverCachePath;
        ReviewDisplay = item.ReviewScorePercent.HasValue
            ? $"{item.ReviewScorePercent}% ({item.ReviewCount ?? 0} reviews)"
            : "—";
        PlayersDisplay = item.CurrentPlayers.HasValue ? $"{item.CurrentPlayers:N0} playing" : "";
        Source = item;
    }

    public int SteamAppId { get; }
    public string Title { get; }
    public CompatibilityTier Tier { get; }
    public LaunchReadinessState Readiness { get; }
    public string? CoverPath { get; }
    public string ReviewDisplay { get; }
    public string PlayersDisplay { get; }
    public GameCatalogItem Source { get; }
}
