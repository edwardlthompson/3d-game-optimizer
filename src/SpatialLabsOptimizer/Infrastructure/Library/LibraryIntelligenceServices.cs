using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Launch;

namespace SpatialLabsOptimizer.Infrastructure.Library;

public enum SmartCollectionMode
{
    None,
    FavoritesAndTier,
    NeverPlayedIn3D,
    LocalOnly
}

public sealed record RecentLaunchEntry(
    int StableAppId,
    string Title,
    DateTimeOffset LaunchedAt,
    bool Success,
    string? ErrorCode);

public sealed class CompatibilityNotesRepository
{
    private const string KeyPrefix = "compat_note:";

    private readonly SqliteSettingsStore _settings;

    public CompatibilityNotesRepository(SqliteSettingsStore settings)
    {
        _settings = settings;
    }

    public Task<string?> GetNoteAsync(int appId, CancellationToken cancellationToken = default)
        => _settings.GetAsync($"{KeyPrefix}{appId}", cancellationToken);

    public Task SaveNoteAsync(int appId, string note, CancellationToken cancellationToken = default)
        => _settings.SetAsync($"{KeyPrefix}{appId}", note.Trim(), cancellationToken);
}

public sealed class LibraryIntelligenceService
{
    private readonly GameDatabase _database;
    private readonly PresetCacheService _presets;
    private readonly CompatibilityNotesRepository _notes;

    public LibraryIntelligenceService(
        GameDatabase database,
        PresetCacheService presets,
        CompatibilityNotesRepository notes)
    {
        _database = database;
        _presets = presets;
        _notes = notes;
    }

    public async Task<IReadOnlyList<RecentLaunchEntry>> GetRecentLaunchesAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        var rows = await _database.GetRecentLaunchesAsync(limit, cancellationToken);
        return rows.Select(r => new RecentLaunchEntry(
            r.StableAppId, r.Title, r.LaunchedAt, r.Success, r.ErrorCode)).ToList();
    }

    public Task RecordLaunchAsync(
        int appId,
        string title,
        bool success,
        string? errorCode = null,
        CancellationToken cancellationToken = default)
        => _database.RecordRecentLaunchAsync(appId, title, success, errorCode, cancellationToken);

    public Task<string?> GetCompatibilityNoteAsync(int appId, CancellationToken cancellationToken = default)
        => _notes.GetNoteAsync(appId, cancellationToken);

    public Task SaveCompatibilityNoteAsync(int appId, string note, CancellationToken cancellationToken = default)
        => _notes.SaveNoteAsync(appId, note, cancellationToken);

    public IReadOnlyList<GameCatalogItem> ApplySmartCollection(
        IReadOnlyList<GameCatalogItem> games,
        SmartCollectionMode mode)
    {
        return mode switch
        {
            SmartCollectionMode.FavoritesAndTier => games
                .Where(g => g.IsFavorite && g.Tier <= CompatibilityTier.Playable)
                .ToList(),
            SmartCollectionMode.NeverPlayedIn3D => games
                .Where(g => string.IsNullOrWhiteSpace(g.ReviewDescriptor) || g.ReviewDescriptor != "played-3d")
                .ToList(),
            SmartCollectionMode.LocalOnly => games
                .Where(g => string.Equals(g.ReviewDescriptor, "Local", StringComparison.OrdinalIgnoreCase))
                .ToList(),
            _ => games
        };
    }

    public IReadOnlyList<GameCatalogItem> ApplyWhyNotReadyFilter(IReadOnlyList<GameCatalogItem> games)
        => games.Where(g => g.Readiness != LaunchReadinessState.Ready).ToList();

    public async Task<string> GetPresetFreshnessLabelAsync(int appId, CancellationToken cancellationToken = default)
    {
        if (!await _presets.HasPresetAsync(appId, cancellationToken))
        {
            return "No preset cached";
        }

        var age = _presets.GetCachedPresetAge(appId);
        if (age is null)
        {
            return "Preset from manifest";
        }

        return age.Value.TotalDays > 30 ? "Preset stale (>30d)" : "Preset fresh";
    }

    public static string GetCompatibilityBadge(CompatibilityTier tier, LaunchReadinessState readiness, bool isLocal)
    {
        if (isLocal)
        {
            return "Local";
        }

        if (readiness == LaunchReadinessState.Ready && tier <= CompatibilityTier.Optimized)
        {
            return "Verified";
        }

        if (readiness == LaunchReadinessState.NeedsPresetCache)
        {
            return "Needs preset";
        }

        return tier switch
        {
            CompatibilityTier.Experimental => "Experimental",
            CompatibilityTier.Unsupported => "Unknown",
            _ => tier.ToString()
        };
    }
}
