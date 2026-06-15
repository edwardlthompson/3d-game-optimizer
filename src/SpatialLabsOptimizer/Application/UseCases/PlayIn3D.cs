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
}
