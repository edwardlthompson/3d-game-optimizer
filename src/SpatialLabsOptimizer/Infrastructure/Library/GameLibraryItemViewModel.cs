using System.ComponentModel;
using System.Runtime.CompilerServices;
using SpatialLabsOptimizer.Domain;

namespace SpatialLabsOptimizer.Infrastructure.Library;

public sealed class GameLibraryItemViewModel : INotifyPropertyChanged
{
    public const string GameRankTooltipText =
        "Game Rank = 72% weighted Steam score + 28% 3D Rank. Steam score shrinks review % for low vote counts and boosts titles with more players. 3D Rank is the single best play path.";

    private string? _coverPath;
    private long _coverRevision;

    public GameLibraryItemViewModel(
        GameCatalogItem item,
        bool isPinned = false,
        string? compatibilityBadge = null,
        string? presetFreshness = null,
        string? sourceBadges = null,
        int rank3DScore = 0,
        string? rank3DLabel = null,
        double? gameRankScore = null)
    {
        SteamAppId = item.SteamAppId;
        Title = item.Title;
        Tier = item.Tier;
        Readiness = item.Readiness;
        _coverPath = item.CoverCachePath;
        IsFavorite = item.IsFavorite;
        IsPinned = isPinned;
        IsLocal = string.Equals(item.ReviewDescriptor, "Local", StringComparison.OrdinalIgnoreCase);
        CompatibilityBadge = compatibilityBadge ?? LibraryIntelligenceService.GetCompatibilityBadge(item.Tier, item.Readiness, IsLocal);
        SourceBadges = sourceBadges ?? "";
        PresetFreshness = presetFreshness ?? "";
        StatusDisplay = string.Join(" · ", new[]
        {
            IsLocal ? "Local" : null,
            item.IsFavorite ? "Favorite" : null,
            isPinned ? "Pinned" : null,
        }.Where(s => s is not null));
        ReviewDisplay = item.ReviewScorePercent.HasValue
            ? $"{item.ReviewScorePercent}% ({item.ReviewCount ?? 0} reviews)"
            : IsLocal ? "Local install" : "—";
        PlayersDisplay = item.CurrentPlayers.HasValue ? $"{item.CurrentPlayers:N0} playing" : "";
        Rank3DScore = rank3DScore;
        Rank3DLabel = rank3DLabel ?? "";
        GameRankScore = gameRankScore;
        GameRankDisplay = gameRankScore is { } score ? $"Game Rank {score:0.0}" : "";
        Source = item;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public int SteamAppId { get; }
    public string Title { get; }
    public CompatibilityTier Tier { get; }
    public LaunchReadinessState Readiness { get; }
    public bool IsFavorite { get; }
    public bool IsPinned { get; }
    public bool IsLocal { get; }
    public string CompatibilityBadge { get; }
    public string SourceBadges { get; }
    public string PresetFreshness { get; }
    public string StatusDisplay { get; }
    public string ReviewDisplay { get; }
    public string PlayersDisplay { get; }
    public int Rank3DScore { get; }
    public string Rank3DLabel { get; }
    public double? GameRankScore { get; }
    public string GameRankDisplay { get; }
    public string GameRankTooltip => GameRankTooltipText;
    public GameCatalogItem Source { get; }

    public string? CoverPath
    {
        get => _coverPath;
        private set
        {
            if (_coverPath == value)
            {
                return;
            }

            _coverPath = value;
            NotifyCoverChanged();
        }
    }

    public long CoverRevision => _coverRevision;

    public string? CoverImageKey => string.IsNullOrWhiteSpace(_coverPath)
        ? null
        : $"{_coverPath}|{_coverRevision}";

    public void UpdateCover(string? path)
    {
        if (_coverPath == path)
        {
            _coverRevision = DateTimeOffset.UtcNow.Ticks;
            OnPropertyChanged(nameof(CoverRevision));
            OnPropertyChanged(nameof(CoverImageKey));
            return;
        }

        CoverPath = path;
    }

    private void NotifyCoverChanged()
    {
        _coverRevision = DateTimeOffset.UtcNow.Ticks;
        OnPropertyChanged(nameof(CoverPath));
        OnPropertyChanged(nameof(CoverRevision));
        OnPropertyChanged(nameof(CoverImageKey));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
