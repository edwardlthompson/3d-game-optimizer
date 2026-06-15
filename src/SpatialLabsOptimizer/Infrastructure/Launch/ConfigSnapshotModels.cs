using SpatialLabsOptimizer.Domain;

namespace SpatialLabsOptimizer.Infrastructure.Launch;

public sealed record ConfigSnapshotEntry(int AppId, string Path, DateTimeOffset CreatedAt);

public sealed class ConfigSnapshotPayload
{
    public int AppId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public SnapshotOverrideData? Override { get; init; }

    public static ConfigSnapshotPayload FromOverride(int appId, GameOverride? entry) => new()
    {
        AppId = appId,
        CreatedAt = DateTimeOffset.UtcNow,
        Override = entry is null ? null : SnapshotOverrideData.From(entry)
    };

    public GameOverride? ToOverride()
    {
        if (Override is null)
        {
            return null;
        }

        LaunchPlatform? platform = null;
        if (!string.IsNullOrWhiteSpace(Override.PlatformOverride) &&
            Enum.TryParse<LaunchPlatform>(Override.PlatformOverride, out var parsed))
        {
            platform = parsed;
        }

        return new GameOverride(
            AppId,
            Override.Depth,
            Override.Convergence,
            platform,
            Override.SafeLaunch,
            Override.PreferredOutput ?? "Auto");
    }
}

public sealed class SnapshotOverrideData
{
    public double? Depth { get; init; }
    public double? Convergence { get; init; }
    public string? PlatformOverride { get; init; }
    public bool SafeLaunch { get; init; }
    public string? PreferredOutput { get; init; }

    public static SnapshotOverrideData From(GameOverride entry) => new()
    {
        Depth = entry.Depth,
        Convergence = entry.Convergence,
        PlatformOverride = entry.PlatformOverride?.ToString(),
        SafeLaunch = entry.SafeLaunch,
        PreferredOutput = entry.PreferredOutput
    };
}
