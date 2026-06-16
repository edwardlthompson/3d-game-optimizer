using SpatialLabsOptimizer.Infrastructure.Artwork;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Privacy;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Steam;

namespace SpatialLabsOptimizer.Tests;

/// <summary>
/// Live-network smoke tests for Steam cover art download (production DI wiring).
/// </summary>
public sealed class CoverArtSmokeTests
{
    [Fact]
    public async Task ResolveCoverPathAsync_DownloadsFromSteamCdn_WithProductionHandler()
    {
        var cacheDir = Path.Combine(Path.GetTempPath(), $"3dgo-cover-smoke-{Guid.NewGuid()}");
        var cache = new CoverArtCache(cacheDir);
        var hub = new OperationProgressHub();
        var handler = new PrivacyGuardHttpHandler(new PrivacyGuard(PrivacyAllowlist.DefaultHosts));
        Assert.NotNull(handler.InnerHandler);

        var gateway = new ExternalDataGateway(handler, hub);
        var store = new SteamStoreApiClient(gateway);
        var artwork = new GameArtworkService(store, gateway, cache, hub);

        var path = await artwork.ResolveCoverPathAsync(570);

        Assert.NotNull(path);
        Assert.True(File.Exists(path), $"Expected cover file at {path}");
        Assert.True(new FileInfo(path).Length > 1000, "Downloaded cover should be a real JPEG");
    }

    [Fact]
    public async Task PrefetchService_DownloadsCover_WithProductionHandler()
    {
        var cacheDir = Path.Combine(Path.GetTempPath(), $"3dgo-prefetch-smoke-{Guid.NewGuid()}");
        var dbPath = Path.Combine(Path.GetTempPath(), $"3dgo-db-smoke-{Guid.NewGuid()}.db");
        var cache = new CoverArtCache(cacheDir);
        var hub = new OperationProgressHub();
        var handler = new PrivacyGuardHttpHandler(new PrivacyGuard(PrivacyAllowlist.DefaultHosts));
        var gateway = new ExternalDataGateway(handler, hub);
        var store = new SteamStoreApiClient(gateway);
        var artwork = new GameArtworkService(store, gateway, cache, hub);
        var database = new GameDatabase(dbPath);
        await database.InitializeAsync();
        await database.UpsertGameAsync(new Domain.GameCatalogItem(
            570,
            "Dota 2",
            Domain.CompatibilityTier.Playable,
            Domain.LaunchReadinessState.Ready,
            true,
            null,
            null,
            null,
            null,
            null,
            null,
            false,
            true));

        var prefetch = new Infrastructure.Library.LibraryPrefetchService(database, artwork, hub);
        await prefetch.PrefetchMissingArtworkAsync([570]);

        var game = await database.GetGameAsync(570);
        Assert.NotNull(game?.CoverCachePath);
        Assert.True(File.Exists(game!.CoverCachePath!), game.CoverCachePath);
    }

    [Fact]
    public async Task ResolveCoverPathAsync_WritesToUserCache_WhenSmokeFlagSet()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("SLO_COVER_SMOKE_USER_CACHE"), "1", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var cache = new CoverArtCache();
        var hub = new OperationProgressHub();
        var handler = new PrivacyGuardHttpHandler(new PrivacyGuard(PrivacyAllowlist.DefaultHosts));
        var gateway = new ExternalDataGateway(handler, hub);
        var store = new SteamStoreApiClient(gateway);
        var artwork = new GameArtworkService(store, gateway, cache, hub);

        foreach (var appId in new[] { 570, 1091500, 1086940 })
        {
            var path = await artwork.ResolveCoverPathAsync(appId);
            Assert.NotNull(path);
            Assert.True(File.Exists(path), $"Missing cover for {appId} at {path}");
        }
    }
}
