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

public sealed class RunSilentSetup
{
    private readonly SilentInstallOrchestrator _installer;
    private readonly OptimalDefaultsService _defaults;
    private readonly DisplayAutoDetector _detector;
    private readonly PresetCacheService _presetCache;
    private readonly OperationProgressHub _progressHub;

    public RunSilentSetup(
        SilentInstallOrchestrator installer,
        OptimalDefaultsService defaults,
        DisplayAutoDetector detector,
        PresetCacheService presetCache,
        OperationProgressHub progressHub)
    {
        _installer = installer;
        _defaults = defaults;
        _detector = detector;
        _presetCache = presetCache;
        _progressHub = progressHub;
    }

    public async Task<bool> ExecuteAsync(DisplayProfile? selectedProfile = null, CancellationToken cancellationToken = default)
    {
        var profile = selectedProfile ?? await _detector.DetectAsync(cancellationToken);
        if (profile is null)
        {
            return false;
        }

        var adapter = _detector.CreateAdapter(profile);
        await adapter.InstallHubSilentlyAsync(cancellationToken);
        await _installer.ExecuteAsync(profile.RequiredToolIds, cancellationToken);
        await _defaults.ApplyForProfileAsync(profile.RecommendedProfileId, cancellationToken);
        await _presetCache.BulkCacheTopPresetsAsync(50, _progressHub, cancellationToken);
        return true;
    }
}
