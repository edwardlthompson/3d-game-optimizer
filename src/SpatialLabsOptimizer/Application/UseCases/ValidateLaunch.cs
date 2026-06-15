using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Compatibility;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Install;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Launch.Coexistence;
using SpatialLabsOptimizer.Infrastructure.Pcvr;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Library;
using SpatialLabsOptimizer.Infrastructure.Settings;

namespace SpatialLabsOptimizer.Application.UseCases;

public sealed class ValidateLaunch
{
    private readonly LaunchReadinessService _readiness;
    private readonly CompatibilityRepository _compatibility;
    private readonly DisplayAutoDetector _detector;

    public ValidateLaunch(
        LaunchReadinessService readiness,
        CompatibilityRepository compatibility,
        DisplayAutoDetector detector)
    {
        _readiness = readiness;
        _compatibility = compatibility;
        _detector = detector;
    }

    public async Task<LaunchReadinessState> DryRunAsync(int appId, CancellationToken cancellationToken = default)
    {
        var entry = await _compatibility.GetByAppIdAsync(appId, cancellationToken);
        var profile = await _detector.DetectAsync(cancellationToken);
        var adapter = profile is not null ? _detector.CreateAdapter(profile) : null;
        var tier = entry is not null && adapter is not null
            ? _compatibility.GetTierForVendor(entry, adapter.Vendor)
            : CompatibilityTier.Experimental;
        return await _readiness.EvaluateAsync(appId, true, tier, cancellationToken);
    }
}
