using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Artwork;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Library;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Updates;

namespace SpatialLabsOptimizer.Tests;

public class LibraryUxTests
{
    [Fact]
    public async Task SetFavorite_PersistsAcrossUpsert()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"3dgo-fav-{Guid.NewGuid()}.db");
        await using var db = new GameDatabase(dbPath);
        await db.InitializeAsync();

        var item = new GameCatalogItem(
            1091500, "Cyberpunk 2077", CompatibilityTier.Optimized,
            LaunchReadinessState.Ready, true, 12000, 92, 50000, 0.9, null, "RPG", false);
        await db.UpsertGameAsync(item);
        await db.SetFavoriteAsync(1091500, true);

        await db.UpsertGameAsync(item with { CurrentPlayers = 13000 });
        var loaded = await db.GetGameAsync(1091500);

        Assert.NotNull(loaded);
        Assert.True(loaded!.IsFavorite);
        Assert.Equal(13000, loaded.CurrentPlayers);
    }

    [Fact]
    public void FavoritesFilter_ReturnsOnlyFavorites()
    {
        var games = new List<GameCatalogItem>
        {
            new(1, "A", CompatibilityTier.Optimized, LaunchReadinessState.Ready, true, null, null, null, null, null, null, true),
            new(2, "B", CompatibilityTier.Playable, LaunchReadinessState.Ready, true, null, null, null, null, null, null, false),
            new(3, "C", CompatibilityTier.Playable, LaunchReadinessState.Ready, true, null, null, null, null, null, null, true)
        };

        var filtered = games.Where(g => g.IsFavorite).ToList();
        Assert.Equal(2, filtered.Count);
        Assert.All(filtered, g => Assert.True(g.IsFavorite));
    }

    [Fact]
    public void PlayQueue_DequeuePlayNext_ReducesCount()
    {
        var queue = new PlayQueueService();
        queue.Enqueue(570);
        queue.Enqueue(1091500);
        Assert.Equal(2, queue.Count);

        Assert.True(queue.TryDequeue(out var first));
        Assert.Equal(570, first);
        Assert.Equal(1, queue.Count);

        Assert.True(queue.TryDequeue(out var second));
        Assert.Equal(1091500, second);
        Assert.False(queue.TryDequeue(out _));
    }

    [Fact]
    public async Task LocalPlaylistRepository_RoundTripsNamesAndIds()
    {
        var path = Path.Combine(Path.GetTempPath(), $"3dgo-playlist-{Guid.NewGuid()}.db");
        await using var store = new SqliteSettingsStore(path);
        await store.InitializeAsync();
        var repo = new LocalPlaylistRepository(store);

        await repo.SavePlaylistAsync("Friday", new[] { 570, 1091500 });
        var loaded = await repo.LoadPlaylistAsync("Friday");
        var names = await repo.ListPlaylistNamesAsync();

        Assert.Equal(new[] { 570, 1091500 }, loaded);
        Assert.Contains("Friday", names);
    }

    [Fact]
    public async Task GameArtworkService_UsesSteamGridDbFallback_WhenCdnEmpty()
    {
        Environment.SetEnvironmentVariable("STEAMGRIDDB_API_KEY", "test-key");
        try
        {
            var hub = new OperationProgressHub();
        var handler = new GridFallbackMessageHandler();
        var gateway = new ExternalDataGateway(
            new Infrastructure.Privacy.PrivacyGuardHttpHandler(
                new Infrastructure.Privacy.PrivacyGuard(Infrastructure.Privacy.PrivacyAllowlist.DefaultHosts))
            {
                InnerHandler = handler
            },
            hub);
        var cacheDir = Path.Combine(Path.GetTempPath(), $"3dgo-cover-{Guid.NewGuid()}");
        var cache = new CoverArtCache(cacheDir);
        var storeClient = new Infrastructure.Steam.SteamStoreApiClient(gateway);
        var gridClient = new SteamGridDbClient(gateway, cache);
        var artwork = new GameArtworkService(storeClient, gateway, cache, hub, gridClient);

        var path = await artwork.ResolveCoverPathAsync(570);
        Assert.NotNull(path);
        Assert.True(File.Exists(path));
        }
        finally
        {
            Environment.SetEnvironmentVariable("STEAMGRIDDB_API_KEY", null);
        }
    }

    [Fact]
    public async Task IncrementalSteamScan_SkipsWhenWithinThrottle()
    {
        var dataRoot = TestPaths.FindDataRoot();
        var loader = new JsonDataLoader(dataRoot);
        var dbPath = Path.Combine(Path.GetTempPath(), $"3dgo-throttle-{Guid.NewGuid()}.db");
        await using var db = new GameDatabase(dbPath);
        var compat = new Infrastructure.Compatibility.CompatibilityRepository(loader);
        var scanner = new Infrastructure.Steam.SteamVdfScanner();
        var hub = new OperationProgressHub();
        var detector = TestPaths.CreateDisplayAutoDetector();
        var handler = new Infrastructure.Privacy.PrivacyGuardHttpHandler(
            new Infrastructure.Privacy.PrivacyGuard(Infrastructure.Privacy.PrivacyAllowlist.DefaultHosts))
        {
            InnerHandler = new StubMessageHandler()
        };
        var gateway = new ExternalDataGateway(handler, hub);
        var artwork = new Infrastructure.Artwork.GameArtworkService(
            new Infrastructure.Steam.SteamStoreApiClient(gateway),
            gateway,
            new CoverArtCache(),
            hub);
        var presets = new PresetCacheService(loader, gateway);
        var readiness = new LaunchReadinessService(presets);
        var external = new LibraryExternalGamesMerger(readiness, db);
        var steamOwned = new LibrarySteamOwnedMerger(readiness, db);
        var placeholders = new LibraryStorePlaceholderAssigner(db, hub);
        var merger = new LibraryIndexMerger(external, steamOwned, placeholders);
        var settingsPath = Path.Combine(Path.GetTempPath(), $"3dgo-throttle-settings-{Guid.NewGuid()}.db");
        await using var settings = new SqliteSettingsStore(settingsPath);
        await settings.InitializeAsync();
        var prefetch = new LibraryPrefetchService(db, artwork, hub);
        var indexer = new LibraryIndexer(
            compat, scanner, readiness, db, hub, detector, merger, prefetch, settings);
        await settings.SetAsync(
            "last_incremental_scan_utc",
            DateTimeOffset.UtcNow.ToString("O"));

        var service = new IncrementalSteamScanService(scanner, db, indexer, hub, settings);
        var delta = await service.ScanNewGamesAsync(force: false);

        Assert.Equal(0, delta);
    }

    [Fact]
    public async Task LibraryIndexer_SkipsFullIndex_WhenRecentlyIndexed()
    {
        var dataRoot = TestPaths.FindDataRoot();
        var loader = new JsonDataLoader(dataRoot);
        var dbPath = Path.Combine(Path.GetTempPath(), $"3dgo-index-skip-{Guid.NewGuid()}.db");
        await using var db = new GameDatabase(dbPath);
        await db.InitializeAsync();
        await db.UpsertGameAsync(new GameCatalogItem(
            570, "Dota 2", CompatibilityTier.Optimized, LaunchReadinessState.Ready,
            true, null, null, null, null, null, null, false, true));
        var settingsPath = Path.Combine(Path.GetTempPath(), $"3dgo-index-skip-settings-{Guid.NewGuid()}.db");
        await using var settings = new SqliteSettingsStore(settingsPath);
        await settings.InitializeAsync();
        await settings.SetAsync(
            LibraryIndexer.LastFullIndexUtcKey,
            DateTimeOffset.UtcNow.ToString("O"));

        var compat = new Infrastructure.Compatibility.CompatibilityRepository(loader);
        var scanner = new Infrastructure.Steam.SteamVdfScanner();
        var hub = new OperationProgressHub();
        var detector = TestPaths.CreateDisplayAutoDetector();
        var handler = new Infrastructure.Privacy.PrivacyGuardHttpHandler(
            new Infrastructure.Privacy.PrivacyGuard(Infrastructure.Privacy.PrivacyAllowlist.DefaultHosts))
        {
            InnerHandler = new StubMessageHandler()
        };
        var gateway = new ExternalDataGateway(handler, hub);
        var artwork = new Infrastructure.Artwork.GameArtworkService(
            new Infrastructure.Steam.SteamStoreApiClient(gateway),
            gateway,
            new CoverArtCache(),
            hub);
        var presets = new PresetCacheService(loader, gateway);
        var readiness = new LaunchReadinessService(presets);
        var external = new LibraryExternalGamesMerger(readiness, db);
        var steamOwned = new LibrarySteamOwnedMerger(readiness, db);
        var placeholders = new LibraryStorePlaceholderAssigner(db, hub);
        var merger = new LibraryIndexMerger(external, steamOwned, placeholders);
        var prefetch = new LibraryPrefetchService(db, artwork, hub);
        var indexer = new LibraryIndexer(
            compat, scanner, readiness, db, hub, detector, merger, prefetch, settings);

        Assert.False(await indexer.ShouldRunFullIndexAsync());
    }

    [Fact]
    public async Task GetCompatible3DAsync_ExcludesNonCatalogTitles()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"3dgo-3donly-{Guid.NewGuid()}.db");
        await using var db = new GameDatabase(dbPath);
        await db.InitializeAsync();
        await db.UpsertGameAsync(new GameCatalogItem(
            570, "Dota 2", CompatibilityTier.Optimized, LaunchReadinessState.Ready,
            true, null, 95, 1000, 0.9, null, null, false, true));
        await db.UpsertGameAsync(new GameCatalogItem(
            999999, "Random Steam Game", CompatibilityTier.Experimental, LaunchReadinessState.Ready,
            true, null, null, null, null, null, null, false, false));

        var compatible = await db.GetCompatible3DAsync();

        Assert.Single(compatible);
        Assert.Equal(570, compatible[0].SteamAppId);
    }

    [Fact]
    public void IncrementalSteamScan_CountNewInstalls_ExcludesKnown()
    {
        var installed = new[] { 570, 1091500, 730 };
        var known = new[] { 570, 730 };
        var delta = IncrementalSteamScanService.CountNewInstalls(installed, known);
        Assert.Equal(1, delta);
    }

    [Fact]
    public async Task ActiveDisplayProfile_RoundTrips()
    {
        var path = Path.Combine(Path.GetTempPath(), $"3dgo-display-{Guid.NewGuid()}.db");
        await using var settings = new SqliteSettingsStore(path);
        await settings.InitializeAsync();

        await settings.SetActiveDisplayProfileIdAsync("acer-psv27-2");
        var loaded = await settings.GetActiveDisplayProfileIdAsync();

        Assert.Equal("acer-psv27-2", loaded);
    }

    [Fact]
    public async Task SetupCompletedAt_RoundTrips()
    {
        var path = Path.Combine(Path.GetTempPath(), $"3dgo-setup-{Guid.NewGuid()}.db");
        await using var settings = new SqliteSettingsStore(path);
        await settings.InitializeAsync();
        var completed = new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

        await settings.SetSetupCompletedAtAsync(completed);
        var loaded = await settings.GetSetupCompletedAtAsync();

        Assert.Equal(completed, loaded);
    }

    [Fact]
    public void CoverArtCache_TryGetCached_ReturnsFalseWhenMissing()
    {
        var cacheDir = Path.Combine(Path.GetTempPath(), $"3dgo-cover-miss-{Guid.NewGuid()}");
        var cache = new CoverArtCache(cacheDir);
        Assert.False(cache.TryGetCached(999999, out _));
    }

    private sealed class StubMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            });
    }

    private sealed class GridFallbackMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var url = request.RequestUri?.ToString() ?? "";
            if (url.Contains("steamgriddb.com", StringComparison.OrdinalIgnoreCase))
            {
                var json = """{"data":[{"url":"https://steamcdn-a.akamaihd.net/steam/apps/570/header.jpg"}]}""";
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(json)
                });
            }

            if (url.Contains("steamcdn-a.akamaihd.net", StringComparison.OrdinalIgnoreCase) &&
                url.Contains("header.jpg", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 })
                });
            }

            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(Array.Empty<byte>())
            });
        }
    }
}
