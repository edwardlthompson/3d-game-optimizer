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
    private readonly ExternalToolCoexistenceService _coexistence;
    private readonly GameFirstLaunchOrchestrator _gameFirst;
    private readonly ToolConfigWriter _configWriter;
    private readonly LaunchAuditService _audit;
    private readonly AutoFallbackLaunchService _fallback;
    private readonly ConfigSnapshotService _snapshots;
    private readonly LaunchErrorCatalog _errors;
    private readonly OperationProgressHub _progressHub;
    private readonly PlayInVR _playInVr;
    private readonly SafeLaunchService _safeLaunch;
    private readonly UserPreferencesService _preferences;
    private readonly LaunchPreviewService _launchPreview;
    private readonly LibraryIntelligenceService _libraryIntel;

    public PlayIn3D(
        ResolveGameSettings resolve,
        DisplayAutoDetector detector,
        PresetCacheService presetCache,
        LaunchReadinessService readiness,
        ExternalToolCoexistenceService coexistence,
        GameFirstLaunchOrchestrator gameFirst,
        ToolConfigWriter configWriter,
        ConfigSnapshotService snapshots,
        LaunchErrorCatalog errors,
        OperationProgressHub progressHub,
        LaunchAuditService audit,
        AutoFallbackLaunchService fallback,
        PlayInVR playInVr,
        SafeLaunchService safeLaunch,
        UserPreferencesService preferences,
        LaunchPreviewService launchPreview,
        LibraryIntelligenceService libraryIntel)
    {
        _resolve = resolve;
        _detector = detector;
        _presetCache = presetCache;
        _readiness = readiness;
        _coexistence = coexistence;
        _gameFirst = gameFirst;
        _configWriter = configWriter;
        _snapshots = snapshots;
        _errors = errors;
        _progressHub = progressHub;
        _audit = audit;
        _fallback = fallback;
        _playInVr = playInVr;
        _safeLaunch = safeLaunch;
        _preferences = preferences;
        _launchPreview = launchPreview;
        _libraryIntel = libraryIntel;
    }

    public async Task<(bool Success, string? ErrorCode)> ExecuteAsync(int appId, CancellationToken cancellationToken = default)
    {
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

        if (string.Equals(plan.PreferredOutput, "Headset", StringComparison.OrdinalIgnoreCase))
        {
            var vrLaunched = await _playInVr.ExecuteAsync(appId, cancellationToken);
            return (vrLaunched, vrLaunched ? null : "3DGO-0005");
        }

        var globalSafeLaunch = await _preferences.GetSafeLaunchAsync(cancellationToken);
        if (globalSafeLaunch || plan.SafeLaunch)
        {
            var safe = await _safeLaunch.LaunchAsync(appId, cancellationToken);
            await _audit.LogAsync(appId, plan.Title, LaunchPlatform.ReShade, safe, safe ? null : "3DGO-0005", "safe-launch", cancellationToken);
            await _libraryIntel.RecordLaunchAsync(appId, plan.Title, safe, safe ? null : "3DGO-0005", cancellationToken);
            return (safe, safe ? null : "3DGO-0005");
        }

        var preview = _launchPreview.Summarize(plan);
        var steps = new[]
        {
            _launchPreview.ToProgressMessage(preview),
            "Checking launch readiness…",
            "Ensuring preset cached…",
            "Resolving game settings…",
            "Selecting platform…",
            "Checking trainer compatibility…",
            "Checking mod manager compatibility…",
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

        var trainerCoexistence = await _preferences.GetTrainerCoexistenceAsync(cancellationToken);
        var modManagerCoexistence = await _preferences.GetModManagerCoexistenceAsync(cancellationToken);
        var (shouldBlock, launchContext) = _coexistence.Evaluate(trainerCoexistence, modManagerCoexistence);
        if (shouldBlock)
        {
            _progressHub.Publish(new OperationProgressReport(
                $"play-3d-{appId}",
                Application.Progress.OperationCategory.Launch,
                "Play in 3D",
                _errors.Get("3DGO-0004").Message,
                IsFailed: true,
                ErrorMessage: _errors.Get("3DGO-0004").Message));
            await _audit.LogAsync(
                appId,
                plan.Title,
                plan.Platform,
                false,
                "3DGO-0004",
                string.Join(", ", launchContext.DetectedTools),
                cancellationToken);
            return (false, "3DGO-0004");
        }

        if (!await _presetCache.HasPresetAsync(appId, cancellationToken))
        {
            await _presetCache.CachePresetAsync(appId, cancellationToken);
        }

        var snapshotPath = await _snapshots.SnapshotAsync(appId, cancellationToken);

        if (launchContext.IsGameFirst)
        {
            var gameFirstLaunched = await _gameFirst.LaunchAsync(plan, cancellationToken);
            if (!gameFirstLaunched)
            {
                await _snapshots.RollbackAsync(snapshotPath, cancellationToken);
                await _audit.LogAsync(
                    appId,
                    plan.Title,
                    plan.Platform,
                    false,
                    "3DGO-0005",
                    "game-first",
                    cancellationToken);
                return (false, "3DGO-0005");
            }

            _progressHub.Publish(new OperationProgressReport(
                $"play-3d-{appId}",
                Application.Progress.OperationCategory.Launch,
                "Play in 3D",
                "Launched successfully (game-first)",
                IsComplete: true));
            await _audit.LogAsync(
                appId,
                plan.Title,
                plan.Platform,
                true,
                null,
                $"game-first: {string.Join(", ", launchContext.DetectedTools)}",
                cancellationToken);
            await _libraryIntel.RecordLaunchAsync(appId, plan.Title, true, null, cancellationToken);
            return (true, null);
        }

        var (launched, usedPlatform, fallbackNote) = await _fallback.LaunchWithFallbackAsync(plan, launchContext, cancellationToken);
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
        await _libraryIntel.RecordLaunchAsync(appId, plan.Title, launched, launched ? null : "3DGO-0005", cancellationToken);
        return (launched, launched ? null : "3DGO-0005");
    }
}

public sealed class PlayInVR
{
    private readonly PcvrRuntimeConnector _pcvr;
    private readonly CompatibilityRepository _compatibility;
    private readonly OperationProgressHub _progressHub;

    public PlayInVR(
        PcvrRuntimeConnector pcvr,
        CompatibilityRepository compatibility,
        OperationProgressHub progressHub)
    {
        _pcvr = pcvr;
        _compatibility = compatibility;
        _progressHub = progressHub;
    }

    public async Task<bool> ExecuteAsync(int appId, CancellationToken cancellationToken = default)
    {
        var entry = await _compatibility.GetByAppIdAsync(appId, cancellationToken);
        var launchOptions = entry?.SteamVrLaunchOptions;

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
            _progressHub.Publish(new OperationProgressReport(
                $"play-vr-{appId}",
                Application.Progress.OperationCategory.Launch,
                "Play in VR",
                "No PCVR runtime detected",
                StepIndex: 3,
                TotalSteps: 3,
                IsComplete: true,
                IsFailed: true,
                ErrorMessage: "No PCVR runtime"));
            return false;
        }

        _progressHub.Publish(new OperationProgressReport(
            $"play-vr-{appId}",
            Application.Progress.OperationCategory.Launch,
            "Play in VR",
            $"Delegating to {runtime}",
            StepIndex: 2,
            TotalSteps: 3));

        var launched = runtime.StartsWith("OpenXR", StringComparison.Ordinal)
            ? await _pcvr.LaunchViaOpenXrAsync(appId, launchOptions, cancellationToken)
            : await _pcvr.LaunchViaSteamVrAsync(appId, launchOptions, cancellationToken);
        _progressHub.Publish(new OperationProgressReport(
            $"play-vr-{appId}",
            Application.Progress.OperationCategory.Launch,
            "Play in VR",
            launched ? "VR launch initiated" : "VR launch failed",
            StepIndex: 3,
            TotalSteps: 3,
            IsComplete: launched,
            IsFailed: !launched));

        return launched;
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
