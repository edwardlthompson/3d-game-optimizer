using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Displays;

namespace SpatialLabsOptimizer.Infrastructure.Launch;

public sealed class LaunchPlatformRouter
{
    public LaunchPlatform Route(
        CompatibilityTier tier,
        IDisplayVendorAdapter vendorAdapter,
        LaunchPlatform? platformOverride = null)
    {
        if (platformOverride.HasValue)
        {
            return platformOverride.Value;
        }

        if (tier >= CompatibilityTier.Unsupported)
        {
            return LaunchPlatform.Blocked;
        }

        return vendorAdapter.GetPreferredLaunchPlatform(tier);
    }
}
