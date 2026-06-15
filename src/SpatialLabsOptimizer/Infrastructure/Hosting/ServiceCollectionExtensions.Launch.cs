using Microsoft.Extensions.DependencyInjection;
using SpatialLabsOptimizer.Infrastructure.Install;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Launch.Coexistence;

namespace SpatialLabsOptimizer.Infrastructure.Hosting;

public static partial class ServiceCollectionExtensions
{
    private static void RegisterLaunchServices(IServiceCollection services)
    {
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
    }
}
