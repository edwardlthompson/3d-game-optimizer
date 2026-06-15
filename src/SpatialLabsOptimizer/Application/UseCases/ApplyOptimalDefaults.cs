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

public sealed class ApplyOptimalDefaults
{
    private readonly OptimalDefaultsService _service;
    private readonly DisplayAutoDetector _detector;

    public ApplyOptimalDefaults(OptimalDefaultsService service, DisplayAutoDetector detector)
    {
        _service = service;
        _detector = detector;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var profile = await _detector.DetectAsync(cancellationToken);
        if (profile is not null)
        {
            await _service.ApplyForProfileAsync(profile.RecommendedProfileId, cancellationToken);
        }
    }
}
