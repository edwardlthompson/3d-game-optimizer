using SpatialLabsOptimizer.Domain;

namespace SpatialLabsOptimizer.Infrastructure.Library;

public enum LibrarySortMode
{
    GameRank,
    PlayersOnline,
    SteamReviews,
    Name,
    Genre
}

public sealed class LibrarySortService
{
    public IReadOnlyList<GameCatalogItem> Sort(IReadOnlyList<GameCatalogItem> games, LibrarySortMode mode) => mode switch
    {
        LibrarySortMode.PlayersOnline => games.OrderByDescending(g => g.CurrentPlayers ?? 0).ThenBy(g => g.Title).ToList(),
        LibrarySortMode.SteamReviews => games.OrderByDescending(g => g.ReviewSortScore ?? 0).ThenByDescending(g => g.ReviewCount ?? 0).ToList(),
        LibrarySortMode.Name => games.OrderBy(g => g.Title).ToList(),
        LibrarySortMode.Genre => games.OrderBy(g => g.ReviewDescriptor ?? "ZZZ").ThenBy(g => g.Title).ToList(),
        _ => games.OrderBy(g => (int)g.Tier).ThenByDescending(g => g.ReviewSortScore ?? 0).ToList()
    };

    public IReadOnlyList<GameCatalogItem> SortByGameRank(
        IReadOnlyList<GameCatalogItem> games,
        IReadOnlyDictionary<int, double> gameRankScores,
        IReadOnlyDictionary<int, int> rank3DScores) =>
        games
            .OrderByDescending(g => gameRankScores.GetValueOrDefault(g.SteamAppId, double.NegativeInfinity))
            .ThenByDescending(g => rank3DScores.GetValueOrDefault(g.SteamAppId))
            .ThenBy(g => (int)g.Tier)
            .ThenByDescending(g => g.ReviewSortScore ?? 0)
            .ThenBy(g => g.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
}
