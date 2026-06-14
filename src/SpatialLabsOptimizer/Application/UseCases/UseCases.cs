using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Compatibility;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Install;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Pcvr;
using SpatialLabsOptimizer.Infrastructure.Progress;

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
        await _installer.ExecuteAsync(cancellationToken);
        await _defaults.ApplyForProfileAsync(profile.RecommendedProfileId, cancellationToken);
        await _presetCache.BulkCacheTopPresetsAsync(50, _progressHub, cancellationToken);
        return true;
    }
}

public sealed class PlayIn3D
{
    private readonly ResolveGameSettings _resolve;
    private readonly DisplayAutoDetector _detector;
    private readonly PresetCacheService _presetCache;
    private readonly LaunchReadinessService _readiness;
    private readonly TrainerCoexistenceService _trainerService;
    private readonly ToolConfigWriter _configWriter;
    private readonly LaunchAuditService _audit;
    private readonly AutoFallbackLaunchService _fallback;
    private readonly ConfigSnapshotService _snapshots;
    private readonly LaunchErrorCatalog _errors;
    private readonly OperationProgressHub _progressHub;

    public PlayIn3D(
        ResolveGameSettings resolve,
        DisplayAutoDetector detector,
        PresetCacheService presetCache,
        LaunchReadinessService readiness,
        TrainerCoexistenceService trainerService,
        ToolConfigWriter configWriter,
        ConfigSnapshotService snapshots,
        LaunchErrorCatalog errors,
        OperationProgressHub progressHub,
        LaunchAuditService audit,
        AutoFallbackLaunchService fallback)
    {
        _resolve = resolve;
        _detector = detector;
        _presetCache = presetCache;
        _readiness = readiness;
        _trainerService = trainerService;
        _configWriter = configWriter;
        _snapshots = snapshots;
        _errors = errors;
        _progressHub = progressHub;
        _audit = audit;
        _fallback = fallback;
    }

    public async Task<(bool Success, string? ErrorCode)> ExecuteAsync(int appId, CancellationToken cancellationToken = default)
    {
        var steps = new[]
        {
            "Checking launch readiness…",
            "Ensuring preset cached…",
            "Resolving game settings…",
            "Selecting platform…",
            "Checking trainer compatibility…",
            "Applying 3D configs…",
            "Applying display optimal defaults…",
            "Starting game…"
        };

        for (var i = 0; i < steps.Length; i++)
        {
            _progressHub.Publish(new OperationProgressReport(
                $"play-3d-{appId}",
                Application.Progress.OperationCategory.Launch,
                "Play in 3D",
                steps[i],
                StepIndex: i + 1,
                TotalSteps: steps.Length,
                PercentComplete: (i + 1) * 100.0 / steps.Length));
            await Task.Delay(30, cancellationToken);
        }

        var profile = await _detector.DetectAsync(cancellationToken);
        if (profile is null)
        {
            return (false, "3DGO-0002");
        }

        var adapter = _detector.CreateAdapter(profile);
        var plan = await _resolve.ResolveAsync(appId, adapter, cancellationToken);
        if (plan.Platform == LaunchPlatform.Blocked)
        {
            return (false, "3DGO-0003");
        }

        if (!await _presetCache.HasPresetAsync(appId, cancellationToken))
        {
            await _presetCache.CachePresetAsync(appId, cancellationToken);
        }

        var snapshotPath = await _snapshots.SnapshotAsync(appId, cancellationToken);
        if (_trainerService.IsTrainerRunning())
        {
            await _trainerService.PrepareCoexistenceAsync(cancellationToken);
        }

        var (launched, usedPlatform, fallbackNote) = await _fallback.LaunchWithFallbackAsync(plan, cancellationToken);
        if (!launched)
        {
            await _snapshots.RollbackAsync(snapshotPath, cancellationToken);
            _progressHub.Publish(new OperationProgressReport(
                $"play-3d-{appId}",
                Application.Progress.OperationCategory.Launch,
                "Play in 3D",
                "Restoring previous settings…",
                IsFailed: true,
                ErrorMessage: _errors.Get("3DGO-0005").Message));
            await _audit.LogAsync(appId, plan.Title, usedPlatform, false, "3DGO-0005", fallbackNote, cancellationToken);
            return (false, "3DGO-0005");
        }

        _progressHub.Publish(new OperationProgressReport(
            $"play-3d-{appId}",
            Application.Progress.OperationCategory.Launch,
            "Play in 3D",
            launched ? "Launched successfully" : "Launch failed",
            IsComplete: launched,
            IsFailed: !launched,
            ErrorMessage: launched ? null : _errors.Get("3DGO-0005").Message));

        await _audit.LogAsync(appId, plan.Title, usedPlatform, launched, launched ? null : "3DGO-0005", fallbackNote, cancellationToken);
        return (launched, launched ? null : "3DGO-0005");
    }
}

public sealed class PlayInVR
{
    private readonly PcvrRuntimeConnector _pcvr;
    private readonly OperationProgressHub _progressHub;

    public PlayInVR(PcvrRuntimeConnector pcvr, OperationProgressHub progressHub)
    {
        _pcvr = pcvr;
        _progressHub = progressHub;
    }

    public async Task<bool> ExecuteAsync(int appId, CancellationToken cancellationToken = default)
    {
        _progressHub.Publish(new OperationProgressReport(
            $"play-vr-{appId}",
            Application.Progress.OperationCategory.Launch,
            "Play in VR",
            "Probing PCVR runtime…",
            StepIndex: 1,
            TotalSteps: 3));

        var runtime = await _pcvr.DetectRuntimeAsync(cancellationToken);
        if (runtime is null)
        {
            return false;
        }

        _progressHub.Publish(new OperationProgressReport(
            $"play-vr-{appId}",
            Application.Progress.OperationCategory.Launch,
            "Play in VR",
            $"Delegating to {runtime}",
            StepIndex: 2,
            TotalSteps: 3,
            IsComplete: true));

        return true;
    }
}

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
