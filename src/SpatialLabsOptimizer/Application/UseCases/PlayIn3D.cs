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

public sealed partial class PlayIn3D
{
    private const int TotalSteps = 12;

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
    private readonly GameDatabase _database;
    private readonly OptimalDefaultsService _defaults;
    private readonly LaunchDisplayHandoffService _displayHandoff;
    private readonly IGameInstallPathResolver _installPaths;

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
        LibraryIntelligenceService libraryIntel,
        GameDatabase database,
        OptimalDefaultsService defaults,
        LaunchDisplayHandoffService displayHandoff,
        IGameInstallPathResolver installPaths)
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
        _database = database;
        _defaults = defaults;
        _displayHandoff = displayHandoff;
        _installPaths = installPaths;
    }

    public async Task<(bool Success, string? ErrorCode)> ExecuteAsync(int appId, CancellationToken cancellationToken = default)
    {
        var step = 0;
        void Publish(
            string message,
            bool complete = false,
            bool failed = false,
            string? error = null)
        {
            step++;
            _progressHub.Publish(new OperationProgressReport(
                $"play-3d-{appId}",
                Application.Progress.OperationCategory.Launch,
                "Play in 3D",
                message,
                StepIndex: step,
                TotalSteps: TotalSteps,
                PercentComplete: step * 100.0 / TotalSteps,
                IsComplete: complete,
                IsFailed: failed,
                ErrorMessage: error));
        }

        Publish("Detecting display profile…");
        var profile = await _detector.DetectAsync(cancellationToken);
        if (profile is null)
        {
            Publish(_errors.Get("3DGO-0002").Message, failed: true, error: _errors.Get("3DGO-0002").Message);
            return (false, "3DGO-0002");
        }

        var adapter = _detector.CreateAdapter(profile);
        Publish("Resolving game settings…");
        var plan = await _resolve.ResolveAsync(appId, adapter, cancellationToken);
        if (plan.Platform == LaunchPlatform.Blocked)
        {
            Publish(_errors.Get("3DGO-0003").Message, failed: true, error: _errors.Get("3DGO-0003").Message);
            return (false, "3DGO-0003");
        }

        var preview = _launchPreview.Summarize(plan);
        Publish(_launchPreview.ToProgressMessage(preview));

        Publish("Checking launch readiness…");
        var catalogGame = await _database.GetGameAsync(appId, cancellationToken);
        var readiness = await _readiness.EvaluateAsync(
            appId,
            catalogGame?.IsInstalled ?? false,
            plan.Tier,
            cancellationToken);
        if (readiness == LaunchReadinessState.Blocked)
        {
            Publish(_errors.Get("3DGO-0003").Message, failed: true, error: _errors.Get("3DGO-0003").Message);
            return (false, "3DGO-0003");
        }

        if (string.Equals(plan.PreferredOutput, "Headset", StringComparison.OrdinalIgnoreCase))
        {
            Publish("Delegating to PCVR launch path…");
            var vrLaunched = await _playInVr.ExecuteAsync(appId, cancellationToken);
            if (!vrLaunched)
            {
                Publish(_errors.Get("3DGO-0005").Message, failed: true, error: _errors.Get("3DGO-0005").Message);
            }

            return (vrLaunched, vrLaunched ? null : "3DGO-0005");
        }

        var globalSafeLaunch = await _preferences.GetSafeLaunchAsync(cancellationToken);
        if (globalSafeLaunch || plan.SafeLaunch)
        {
            Publish("Starting game (safe launch)…");
            var safe = await _safeLaunch.LaunchAsync(appId, cancellationToken);
            Publish(safe ? "Launched successfully (safe launch)" : _errors.Get("3DGO-0005").Message, complete: safe, failed: !safe);
            await _audit.LogAsync(appId, plan.Title, LaunchPlatform.ReShade, safe, safe ? null : "3DGO-0005", "safe-launch", cancellationToken);
            await _libraryIntel.RecordLaunchAsync(appId, plan.Title, safe, safe ? null : "3DGO-0005", cancellationToken);
            return (safe, safe ? null : "3DGO-0005");
        }

        return await LaunchWithConfigsAsync(appId, plan, profile, Publish, cancellationToken);
    }
}
