using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using SpatialLabsOptimizer.Infrastructure;
using SpatialLabsOptimizer.Infrastructure.Artwork;
using SpatialLabsOptimizer.Infrastructure.Compatibility;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Install;
using SpatialLabsOptimizer.Infrastructure.Performance;
using SpatialLabsOptimizer.Infrastructure.Privacy;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Responsive;
using SpatialLabsOptimizer.Infrastructure.Steam;
using SpatialLabsOptimizer.Infrastructure.Updates;

namespace SpatialLabsOptimizer.Infrastructure.Hosting;

public static partial class ServiceCollectionExtensions
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
                RegisterPrivacyAndData(services);
                RegisterDisplaysAndSteam(services);
                RegisterPerformanceAndInstall(services);
                RegisterLaunchServices(services);
                RegisterLibraryServices(services);
                RegisterExtrasAndUi(services);
            });
    }

    private static void RegisterPrivacyAndData(IServiceCollection services)
    {
        services.AddSingleton(new PrivacyGuard(PrivacyAllowlist.DefaultHosts));
        services.AddTransient<PrivacyGuardHttpHandler>();
        services.AddSingleton<OperationProgressHub>();
        services.AddSingleton<ResponsiveStateService>();
        services.AddSingleton<JsonDataLoader>();
        services.AddSingleton<SqliteSettingsStore>();
        services.AddSingleton<GameDatabase>();
        services.AddSingleton<ExternalDataGateway>();
        services.AddSingleton<CompatibilityRepository>();
    }

    private static void RegisterDisplaysAndSteam(IServiceCollection services)
    {
        services.AddSingleton<IDisplayEdidProbe, WmiDisplayEdidProbe>();
        services.AddSingleton<DisplayAutoDetector>();
        services.AddSingleton<DisplayChangeMonitor>();
        services.AddSingleton<MultiMonitorLaunchPicker>();
        services.AddSingleton<LaunchDisplayHandoffService>();
        services.AddSingleton<SteamStoreApiClient>();
        services.AddSingleton<SteamReviewService>();
        services.AddSingleton<PlayerCountService>();
        services.AddSingleton<SteamVdfScanner>();
        services.AddSingleton<SteamWebApiClient>();
        services.AddSingleton<SteamAppReviewsClient>();
        services.AddSingleton<CoverArtCache>();
        services.AddSingleton<SteamGridDbClient>();
        services.AddSingleton<GameArtworkService>();
    }

    private static void RegisterPerformanceAndInstall(IServiceCollection services)
    {
        services.AddSingleton<SystemSpecsScanner>();
        services.AddSingleton<PerformanceTierEstimator>();
        services.AddSingleton<BenchmarkService>();
        services.AddSingleton<MuxGpuDetector>();
        services.AddSingleton<ViewingDistanceCoach>();
        services.AddSingleton<InstallErrorCatalog>();
        services.AddSingleton<IElevatedHelperLocator, DefaultElevatedHelperLocator>();
        services.AddSingleton<SilentInstallOrchestrator>();
        services.AddSingleton<ToolConfigWriter>();
        services.AddSingleton<OptimalDefaultsService>();
    }
}
