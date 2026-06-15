using Microsoft.Extensions.DependencyInjection;
using SpatialLabsOptimizer.Infrastructure.Artwork;
using SpatialLabsOptimizer.Infrastructure.Compatibility;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Library;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Security;
using SpatialLabsOptimizer.Infrastructure.Steam;
using SpatialLabsOptimizer.Infrastructure.Updates;

namespace SpatialLabsOptimizer.Infrastructure.Hosting;

public static partial class ServiceCollectionExtensions
{
    private static void RegisterLibraryServices(IServiceCollection services)
    {
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
            sp.GetRequiredService<LibraryPrefetchService>(),
            sp.GetRequiredService<SqliteSettingsStore>()));
        services.AddSingleton<LibrarySortService>();
    }
}
