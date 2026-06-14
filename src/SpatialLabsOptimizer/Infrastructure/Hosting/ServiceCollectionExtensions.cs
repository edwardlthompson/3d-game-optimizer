using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using SpatialLabsOptimizer.Application.UseCases;
using SpatialLabsOptimizer.Infrastructure.Artwork;
using SpatialLabsOptimizer.Infrastructure.Compatibility;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Install;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Library;
using SpatialLabsOptimizer.Infrastructure.Pcvr;
using SpatialLabsOptimizer.Infrastructure.Performance;
using SpatialLabsOptimizer.Infrastructure.Privacy;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Responsive;
using SpatialLabsOptimizer.Infrastructure.Steam;
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
                services.AddSingleton<DisplayAutoDetector>();

                // Steam
                services.AddSingleton<SteamStoreApiClient>();
                services.AddSingleton<SteamReviewService>();
                services.AddSingleton<PlayerCountService>();
                services.AddSingleton<SteamVdfScanner>();
                services.AddSingleton<SteamWebApiClient>();

                // Artwork
                services.AddSingleton<CoverArtCache>();
                services.AddSingleton<GameArtworkService>();

                // Performance
                services.AddSingleton<SystemSpecsScanner>();
                services.AddSingleton<PerformanceTierEstimator>();
                services.AddSingleton<BenchmarkService>();
                services.AddSingleton<MuxGpuDetector>();
                services.AddSingleton<ViewingDistanceCoach>();

                // Install
                services.AddSingleton<SilentInstallOrchestrator>();
                services.AddSingleton<ToolConfigWriter>();
                services.AddSingleton<OptimalDefaultsService>();

                // Launch
                services.AddSingleton<PresetCacheService>();
                services.AddSingleton<LaunchReadinessService>();
                services.AddSingleton<LaunchPlatformRouter>();
                services.AddSingleton<GameOverrideRepository>();
                services.AddSingleton<ResolveGameSettings>();
                services.AddSingleton<LaunchErrorCatalog>();
                services.AddSingleton<SafeLaunchService>();
                services.AddSingleton<TrainerCoexistenceService>();
                services.AddSingleton<ModManagerCoexistenceService>();
                services.AddSingleton<ConfigSnapshotService>();
                services.AddSingleton<LaunchAuditService>();
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
                services.AddSingleton<LibraryIndexer>();
                services.AddSingleton<LibrarySortService>();
                services.AddSingleton<PinnedShelfRepository>();
                services.AddSingleton<LocalPlaylistRepository>();

                // PCVR & extras
                services.AddSingleton<PcvrRuntimeConnector>();
                services.AddSingleton<UpdateService>();
                services.AddSingleton<DiagnosticBundleService>();
                services.AddSingleton<CommandPaletteService>();

                // v1.0.1+ / v1.1 / v2 scaffolds
                services.AddSingleton<IncrementalSteamScanService>();
                services.AddSingleton<HdrWatchdogService>();
                services.AddSingleton<PlayQueueService>();
                services.AddSingleton<SessionProfileService>();
                services.AddSingleton<SteamGridDbClient>();
                services.AddSingleton<LanPartyExportService>();
                services.AddSingleton<StreamerHotkeyService>();
                services.AddSingleton<HybridSessionService>();
                services.AddSingleton<ThreeDGoCodeService>();
                services.AddSingleton<ModManagerIntegrationService>();
                services.AddSingleton<UserPreferencesService>();

                // Use cases
                services.AddSingleton<RunSilentSetup>();
                services.AddSingleton<PlayIn3D>();
                services.AddSingleton<PlayInVR>();
                services.AddSingleton<ApplyOptimalDefaults>();
                services.AddSingleton<ValidateLaunch>();

                // UI
                services.AddSingleton<MainWindow>();
                services.AddSingleton<ShellPage>();
                services.AddSingleton<ShellViewModel>();
                services.AddSingleton<GameLibraryViewModel>();
                services.AddSingleton<SetupWizardViewModel>();
            });
    }
}
