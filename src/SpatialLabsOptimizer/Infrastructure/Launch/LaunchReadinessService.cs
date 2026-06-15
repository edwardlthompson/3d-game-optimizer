using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Progress;

namespace SpatialLabsOptimizer.Infrastructure.Launch;

public sealed class LaunchReadinessService
{
    private readonly PresetCacheService _presetCache;

    public LaunchReadinessService(PresetCacheService presetCache)
    {
        _presetCache = presetCache;
    }

    public async Task<LaunchReadinessState> EvaluateAsync(int appId, bool isInstalled, CompatibilityTier tier, CancellationToken cancellationToken = default)
    {
        if (tier >= CompatibilityTier.Unsupported)
        {
            return LaunchReadinessState.Blocked;
        }

        if (!isInstalled)
        {
            return LaunchReadinessState.NeedsInstall;
        }

        if (!await _presetCache.HasPresetAsync(appId, cancellationToken))
        {
            return LaunchReadinessState.NeedsPresetCache;
        }

        return LaunchReadinessState.Ready;
    }
}
