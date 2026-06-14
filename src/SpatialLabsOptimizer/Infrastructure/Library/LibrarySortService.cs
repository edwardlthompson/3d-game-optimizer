using SpatialLabsOptimizer.Domain;

namespace SpatialLabsOptimizer.Infrastructure.Library;

public enum LibrarySortMode
{
    Quality,
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
}

public sealed class GameLibraryItemViewModel
{
    public GameLibraryItemViewModel(
        GameCatalogItem item,
        bool isPinned = false,
        string? compatibilityBadge = null,
        string? presetFreshness = null)
    {
        SteamAppId = item.SteamAppId;
        Title = item.Title;
        Tier = item.Tier;
        Readiness = item.Readiness;
        CoverPath = item.CoverCachePath;
        IsFavorite = item.IsFavorite;
        IsPinned = isPinned;
        IsLocal = string.Equals(item.ReviewDescriptor, "Local", StringComparison.OrdinalIgnoreCase);
        CompatibilityBadge = compatibilityBadge ?? LibraryIntelligenceService.GetCompatibilityBadge(item.Tier, item.Readiness, IsLocal);
        PresetFreshness = presetFreshness ?? "";
        StatusDisplay = string.Join(" · ", new[]
        {
            IsLocal ? "Local" : null,
            item.IsFavorite ? "Favorite" : null,
            isPinned ? "Pinned" : null,
            CompatibilityBadge
        }.Where(s => s is not null));
        ReviewDisplay = item.ReviewScorePercent.HasValue
            ? $"{item.ReviewScorePercent}% ({item.ReviewCount ?? 0} reviews)"
            : IsLocal ? "Local install" : "—";
        PlayersDisplay = item.CurrentPlayers.HasValue ? $"{item.CurrentPlayers:N0} playing" : "";
        Source = item;
    }

    public int SteamAppId { get; }
    public string Title { get; }
    public CompatibilityTier Tier { get; }
    public LaunchReadinessState Readiness { get; }
    public string? CoverPath { get; }
    public bool IsFavorite { get; }
    public bool IsPinned { get; }
    public bool IsLocal { get; }
    public string CompatibilityBadge { get; }
    public string PresetFreshness { get; }
    public string StatusDisplay { get; }
    public string ReviewDisplay { get; }
    public string PlayersDisplay { get; }
    public GameCatalogItem Source { get; }
}
