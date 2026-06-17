using Microsoft.Extensions.DependencyInjection;
using SpatialLabsOptimizer.Application.UseCases;
using SpatialLabsOptimizer.Infrastructure;
using SpatialLabsOptimizer.Infrastructure.Pcvr;
using SpatialLabsOptimizer.Infrastructure.Settings;
using SpatialLabsOptimizer.Infrastructure.Updates;
using SpatialLabsOptimizer.ViewModels;
using SpatialLabsOptimizer.Views;

namespace SpatialLabsOptimizer.Infrastructure.Hosting;

public static partial class ServiceCollectionExtensions
{
    private static void RegisterExtrasAndUi(IServiceCollection services)
    {
        services.AddSingleton<PcvrRuntimeConnector>();
        services.AddSingleton<OpenXrRuntimePicker>();
        services.AddSingleton<InstallArtifactDetector>();
        services.AddSingleton<UpdateService>();
        services.AddSingleton<UpdateDownloadService>();
        services.AddSingleton<IUpdateApplier, ZipUpdateApplier>();
        services.AddSingleton<IUpdateApplier, MsiUpdateApplier>();
        services.AddSingleton<UpdateApplyService>();
        services.AddSingleton<UpdateScheduler>();
        services.AddSingleton<CatalogUpdateService>();
        services.AddSingleton<CatalogUpdateScheduler>();
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

        services.AddSingleton<RunSilentSetup>();
        services.AddSingleton<PlayIn3D>();
        services.AddSingleton<PlayInVR>();
        services.AddSingleton<ApplyOptimalDefaults>();
        services.AddSingleton<ValidateLaunch>();
        services.AddSingleton<ShellPage>();
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<GameLibraryViewModel>();
        services.AddSingleton<LibrarySettingsViewModel>();
        services.AddSingleton<ToolchainSetupViewModel>();
        services.AddSingleton<AboutViewModel>();
        services.AddSingleton<TroubleshootingViewModel>();
        services.AddSingleton<GlossaryViewModel>();
        services.AddSingleton<CommandPaletteViewModel>();
        services.AddSingleton<ToolchainHealthViewModel>();
        services.AddSingleton<Global3DSettingsViewModel>();
    }
}
