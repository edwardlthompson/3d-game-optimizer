using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using SpatialLabsOptimizer.Application.UseCases;
using SpatialLabsOptimizer.Infrastructure;
using SpatialLabsOptimizer.Infrastructure.Artwork;
using SpatialLabsOptimizer.Infrastructure.Compatibility;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Install;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Launch.Coexistence;
using SpatialLabsOptimizer.Infrastructure.Library;
using SpatialLabsOptimizer.Infrastructure.Pcvr;
using SpatialLabsOptimizer.Infrastructure.Performance;
using SpatialLabsOptimizer.Infrastructure.Privacy;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Responsive;
using SpatialLabsOptimizer.Infrastructure.Steam;
using SpatialLabsOptimizer.Infrastructure.Security;
using SpatialLabsOptimizer.Infrastructure.Settings;
using SpatialLabsOptimizer.Infrastructure.Updates;
using SpatialLabsOptimizer.ViewModels;
using SpatialLabsOptimizer.Views;

namespace SpatialLabsOptimizer.Infrastructure.Hosting;

public static class ServiceCollectionExtensions
{
    public static IHostBuilder ConfigureSpatialLabsOptimizerServices(this IHostBuilder hostBuilder)
    {
        return hostBuilder
            .UseSerilog((_, _, config) =>
            {
                var logDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "3d-game-optimizer", "logs");
                Directory.CreateDirectory(logDir);
                config
                    .MinimumLevel.Information()
                    .WriteTo.File(
                        Path.Combine(logDir, "spatiallabs-optimizer-.log"),
                        rollingInterval: RollingInterval.Day);
            })
            .ConfigureServices(services =>
            {
                // Privacy
                services.AddSingleton(new PrivacyGuard(PrivacyAllowlist.DefaultHosts));
                services.AddTransient<PrivacyGuardHttpHandler>();

                // Progress
                services.AddSingleton<OperationProgressHub>();
                services.AddSingleton<ResponsiveStateService>();

                // Data
                services.AddSingleton<JsonDataLoader>();
                services.AddSingleton<SqliteSettingsStore>();
                services.AddSingleton<GameDatabase>();
                services.AddSingleton<ExternalDataGateway>();

                // Compatibility & displays
                services.AddSingleton<CompatibilityRepository>();
                services.AddSingleton<IDisplayEdidProbe, WmiDisplayEdidProbe>();
                services.AddSingleton<DisplayAutoDetector>();
                services.AddSingleton<DisplayChangeMonitor>();
                services.AddSingleton<MultiMonitorLaunchPicker>();
                services.AddSingleton<LaunchDisplayHandoffService>();

                // Steam
                services.AddSingleton<SteamStoreApiClient>();
                services.AddSingleton<SteamReviewService>();
                services.AddSingleton<PlayerCountService>();
                services.AddSingleton<SteamVdfScanner>();
                services.AddSingleton<SteamWebApiClient>();
                services.AddSingleton<SteamAppReviewsClient>();

                // Artwork
                services.AddSingleton<CoverArtCache>();
                services.AddSingleton<SteamGridDbClient>();
                services.AddSingleton<GameArtworkService>();

                // Performance
                services.AddSingleton<SystemSpecsScanner>();
                services.AddSingleton<PerformanceTierEstimator>();
                services.AddSingleton<BenchmarkService>();
                services.AddSingleton<MuxGpuDetector>();
                services.AddSingleton<ViewingDistanceCoach>();

                // Install
                services.AddSingleton<InstallErrorCatalog>();
                services.AddSingleton<IElevatedHelperLocator, DefaultElevatedHelperLocator>();
                services.AddSingleton<SilentInstallOrchestrator>();
                services.AddSingleton<ToolConfigWriter>();
                services.AddSingleton<OptimalDefaultsService>();

                // Launch
                services.AddSingleton<GameInstallPathResolver>();
                services.AddSingleton<LocalGameInstallResolver>();
                services.AddSingleton<IGameInstallPathResolver>(sp => sp.GetRequiredService<LocalGameInstallResolver>());
                services.AddSingleton<IProcessLauncher, ProcessLauncher>();
                services.AddSingleton<ToolPathResolver>();
                services.AddSingleton<ToolInstallDetector>();
                services.AddSingleton<PresetCacheService>();
                services.AddSingleton<LaunchReadinessService>();
                services.AddSingleton<LaunchPlatformRouter>();
                services.AddSingleton<GameOverrideRepository>();
                services.AddSingleton<ResolveGameSettings>();
                services.AddSingleton<LaunchErrorCatalog>();
                services.AddSingleton<SafeLaunchService>();
                services.AddSingleton<IRunningProcessProbe, RunningProcessProbe>();
                services.AddSingleton<ExternalToolCoexistenceService>();
                services.AddSingleton<GameFirstLaunchOrchestrator>();
                services.AddSingleton<ConfigSnapshotService>(sp =>
                    new ConfigSnapshotService(sp.GetRequiredService<GameOverrideRepository>()));
                services.AddSingleton<LaunchAuditService>();
                services.AddSingleton<LaunchPreviewService>();
                services.AddSingleton<AutoFallbackLaunchService>();
                services.AddSingleton<TrueGameLauncher>();
                services.AddSingleton<UevrLauncher>();
                services.AddSingleton<ReShadeLauncher>();
                services.AddSingleton<LaunchAdapterRegistry>(sp => new LaunchAdapterRegistry(new LaunchAdapterBase[]
                {
                    sp.GetRequiredService<TrueGameLauncher>(),
                    sp.GetRequiredService<UevrLauncher>(),
                    sp.GetRequiredService<ReShadeLauncher>()
                }));

                // Library
                services.AddSingleton<PinnedShelfRepository>();
                services.AddSingleton<LocalPlaylistRepository>();
                services.AddSingleton<LocalGameFolderRepository>();
                services.AddSingleton<LocalFolderGameScanner>();
                services.AddSingleton<CompatibilityNotesRepository>();
                services.AddSingleton<LibraryIntelligenceService>();
                services.AddSingleton<DpapiSecretStore>();
                services.AddSingleton<PlatformConnectionRepository>();
                services.AddSingleton<PlatformConnectionService>();
                services.AddSingleton<PlatformLibraryStatsService>();
                services.AddSingleton<EpicGogLibraryScanner>();
                services.AddSingleton<UbisoftConnectScanner>();
                services.AddSingleton<LibraryExternalGamesMerger>(sp => new LibraryExternalGamesMerger(
                    sp.GetRequiredService<LaunchReadinessService>(),
                    sp.GetRequiredService<GameDatabase>(),
                    sp.GetService<EpicGogLibraryScanner>(),
                    sp.GetRequiredService<LocalGameFolderRepository>(),
                    sp.GetRequiredService<LocalFolderGameScanner>(),
                    sp.GetRequiredService<LocalGameInstallResolver>(),
                    sp.GetRequiredService<UbisoftConnectScanner>()));
                services.AddSingleton<LibrarySteamOwnedMerger>(sp => new LibrarySteamOwnedMerger(
                    sp.GetRequiredService<LaunchReadinessService>(),
                    sp.GetRequiredService<GameDatabase>(),
                    sp.GetRequiredService<PlatformConnectionRepository>(),
                    sp.GetRequiredService<SteamWebApiClient>(),
                    sp.GetRequiredService<SteamStoreApiClient>()));
                services.AddSingleton<LibraryStorePlaceholderAssigner>(sp => new LibraryStorePlaceholderAssigner(
                    sp.GetRequiredService<GameDatabase>(),
                    sp.GetRequiredService<OperationProgressHub>()));
                services.AddSingleton<LibraryIndexMerger>(sp => new LibraryIndexMerger(
                    sp.GetRequiredService<LibraryExternalGamesMerger>(),
                    sp.GetRequiredService<LibrarySteamOwnedMerger>(),
                    sp.GetRequiredService<LibraryStorePlaceholderAssigner>()));
                services.AddSingleton<LibraryPrefetchService>(sp => new LibraryPrefetchService(
                    sp.GetRequiredService<GameDatabase>(),
                    sp.GetRequiredService<GameArtworkService>(),
                    sp.GetRequiredService<OperationProgressHub>(),
                    sp.GetRequiredService<PlatformConnectionRepository>(),
                    sp.GetRequiredService<SteamAppReviewsClient>(),
                    sp.GetRequiredService<PlayerCountService>()));
                services.AddSingleton<LibraryIndexer>(sp => new LibraryIndexer(
                    sp.GetRequiredService<CompatibilityRepository>(),
                    sp.GetRequiredService<SteamVdfScanner>(),
                    sp.GetRequiredService<LaunchReadinessService>(),
                    sp.GetRequiredService<GameDatabase>(),
                    sp.GetRequiredService<OperationProgressHub>(),
                    sp.GetRequiredService<DisplayAutoDetector>(),
                    sp.GetRequiredService<LibraryIndexMerger>(),
                    sp.GetRequiredService<LibraryPrefetchService>()));
                services.AddSingleton<LibrarySortService>();

                // PCVR & extras (always on)
                services.AddSingleton<PcvrRuntimeConnector>();
                services.AddSingleton<OpenXrRuntimePicker>();
                services.AddSingleton<InstallArtifactDetector>();
                services.AddSingleton<UpdateService>();
                services.AddSingleton<UpdateDownloadService>();
                services.AddSingleton<IUpdateApplier, ZipUpdateApplier>();
                services.AddSingleton<IUpdateApplier, MsiUpdateApplier>();
                services.AddSingleton<IUpdateApplier, MsixUpdateApplier>();
                services.AddSingleton<UpdateApplyService>();
                services.AddSingleton<UpdateScheduler>();
                services.AddSingleton<DiagnosticBundleService>();
                services.AddSingleton<LaunchDryRunService>();
                services.AddSingleton<ReadinessScoreService>();
                services.AddSingleton<SeedContributionExportService>();
                services.AddSingleton<ProtocolRegistrationService>();
                services.AddSingleton<CommandPaletteService>();
                services.AddSingleton<PlayQueueService>();
                services.AddSingleton<UserPreferencesService>();

                if (FeatureFlags.V101Enabled)
                {
                    services.AddSingleton<IncrementalSteamScanService>();
                    services.AddSingleton<HdrWatchdogService>();
                }

                if (FeatureFlags.V11Enabled)
                {
                    services.AddSingleton<SessionProfileService>();
                    services.AddSingleton<StreamFriendlyProfileService>();
                    services.AddSingleton<StreamerHotkeyService>();
                    services.AddSingleton<ModManagerIntegrationService>();
                }

                FeatureFlags.V2RegisteredAtStartup = FeatureFlags.V2Enabled;

                if (FeatureFlags.V2RegisteredAtStartup)
                {
                    services.AddSingleton<LanPartyExportService>();
                    services.AddSingleton<LanPresetExportService>();
                    services.AddSingleton<HybridSessionService>();
                    services.AddSingleton<ThreeDGoCodeService>();
                    services.AddSingleton<WorkshopPresetImporter>();
                }

                // Use cases
                services.AddSingleton<RunSilentSetup>();
                services.AddSingleton<PlayIn3D>();
                services.AddSingleton<PlayInVR>();
                services.AddSingleton<ApplyOptimalDefaults>();
                services.AddSingleton<ValidateLaunch>();

                // UI
                services.AddSingleton<ShellPage>();
                services.AddSingleton<ShellViewModel>();
                services.AddSingleton<GameLibraryViewModel>();
                services.AddSingleton<LibrarySettingsViewModel>();
                services.AddSingleton<SetupWizardViewModel>();
                services.AddSingleton<AboutViewModel>();
                services.AddSingleton<TroubleshootingViewModel>();
                services.AddSingleton<GlossaryViewModel>();
                services.AddSingleton<Global3DSettingsViewModel>();
            });
    }
}
