using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Compatibility;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Install;

namespace SpatialLabsOptimizer.Infrastructure.Launch;

public sealed class ResolveGameSettings
{
    private readonly OptimalDefaultsService _defaults;
    private readonly GameOverrideRepository _overrides;
    private readonly CompatibilityRepository _compatibility;
    private readonly LaunchPlatformRouter _router;

    public ResolveGameSettings(
        OptimalDefaultsService defaults,
        GameOverrideRepository overrides,
        CompatibilityRepository compatibility,
        LaunchPlatformRouter router)
    {
        _defaults = defaults;
        _overrides = overrides;
        _compatibility = compatibility;
        _router = router;
    }

    public async Task<ResolvedGameLaunchPlan> ResolveAsync(
        int appId,
        IDisplayVendorAdapter vendorAdapter,
        CancellationToken cancellationToken = default)
    {
        var entry = await _compatibility.GetByAppIdAsync(appId, cancellationToken);
        var tier = entry is not null
            ? _compatibility.GetTierForVendor(entry, vendorAdapter.Vendor)
            : CompatibilityTier.Experimental;

        var gameOverride = await _overrides.GetAsync(appId, cancellationToken);
        var platform = _router.Route(tier, vendorAdapter, gameOverride?.PlatformOverride);
        var safeLaunch = gameOverride?.SafeLaunch ?? false;
        var preferredOutput = gameOverride?.PreferredOutput ?? "Auto";

        return new ResolvedGameLaunchPlan(
            appId,
            entry?.Title ?? $"App {appId}",
            platform,
            tier,
            gameOverride?.Depth ?? 0.65,
            gameOverride?.Convergence ?? 0.5,
            0.7,
            null,
            null,
            safeLaunch,
            preferredOutput);
    }
}
