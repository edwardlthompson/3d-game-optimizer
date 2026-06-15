using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Artwork;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Library;
using SpatialLabsOptimizer.Infrastructure.Progress;
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
