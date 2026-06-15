using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Install;

namespace SpatialLabsOptimizer.Tests;

public sealed class Sprint46Tests
{
    [Fact]
    public async Task OptimalDefaultsService_DeserializesSeedFile_AndAppliesProfile()
    {
        var loader = new JsonDataLoader(TestPaths.FindDataRoot());
        var writer = new ToolConfigWriter();
        var service = new OptimalDefaultsService(loader, writer);

        var ex = await Record.ExceptionAsync(() =>
            service.ApplyForProfileAsync("profile-acer-spatiallabs-view-15"));

        Assert.Null(ex);
    }

    [Fact]
    public async Task OptimalDefaultsService_AllCatalogProfileIdsExistInSeed()
    {
        var dataRoot = TestPaths.FindDataRoot();
        var loader = new JsonDataLoader(dataRoot);
        var detector = TestPaths.CreateDisplayAutoDetector();
        var catalog = await detector.GetCatalogAsync();
        var defaults = await loader.LoadAsync<OptimalDefaultsSeed>("defaults/optimal-displays-v1.json");

        Assert.NotNull(defaults?.Profiles);
        var ids = defaults!.Profiles.Select(p => p.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var display in catalog.Where(d => d.Id != "generic-manual"))
        {
            Assert.True(ids.Contains(display.RecommendedProfileId),
                $"Missing optimal profile for {display.Id}: {display.RecommendedProfileId}");
        }
    }

    [Fact]
    public async Task DetectAsync_MatchesAsv15_ByNameHint()
    {
        var probe = new FakeDisplayEdidProbe(() =>
        [
            new DisplayEdidSnapshot("display1", "5986:abcd", "Acer SpatialLabs View Pro 15.6")
        ]);
        var loader = new JsonDataLoader(TestPaths.FindDataRoot());
        var detector = new DisplayAutoDetector(loader, probe);
        var profile = await detector.DetectAsync();

        Assert.NotNull(profile);
        Assert.Equal("acer-asv15-1", profile!.Id);
    }

    [Fact]
    public async Task DetectAsync_LaptopSignature_DoesNotMatchAsv15()
    {
        var probe = new FakeDisplayEdidProbe(() =>
        [
            new DisplayEdidSnapshot("display1", "1022:abcd", "Acer SpatialLabs 15 Laptop Panel")
        ]);
        var loader = new JsonDataLoader(TestPaths.FindDataRoot());
        var detector = new DisplayAutoDetector(loader, probe);
        var profile = await detector.DetectAsync();

        Assert.NotNull(profile);
        Assert.Equal("acer-spatiallabs-15", profile!.Id);
    }

    [Fact]
    public async Task GameArtworkService_UsesCdnWhenStoreUnavailable()
    {
        var handler = new CoverArtTestHandler(url =>
            url.Contains("library_600x900", StringComparison.OrdinalIgnoreCase)
                ? new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent([0xFF, 0xD8, 0xFF, 0xDB, 0x00, 0x10])
                }
                : new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));

        var hub = new Infrastructure.Progress.OperationProgressHub();
        var gateway = new ExternalDataGateway(
            new Infrastructure.Privacy.PrivacyGuardHttpHandler(
                new Infrastructure.Privacy.PrivacyGuard(Infrastructure.Privacy.PrivacyAllowlist.DefaultHosts))
            {
                InnerHandler = handler
            },
            hub);
        var cache = new Infrastructure.Artwork.CoverArtCache(
            Path.Combine(Path.GetTempPath(), $"3dgo-cover-{Guid.NewGuid()}"));
        var store = new Infrastructure.Steam.SteamStoreApiClient(gateway);
        var artwork = new Infrastructure.Artwork.GameArtworkService(store, gateway, cache, hub);

        var path = await artwork.ResolveCoverPathAsync(570);

        Assert.NotNull(path);
        Assert.True(File.Exists(path));
    }

    private sealed class OptimalDefaultsSeed
    {
        public List<OptimalProfileSeed> Profiles { get; set; } = [];
    }

    private sealed class OptimalProfileSeed
    {
        public string Id { get; set; } = "";
    }

    private sealed class CoverArtTestHandler : HttpMessageHandler
    {
        private readonly Func<string, HttpResponseMessage> _responder;

        public CoverArtTestHandler(Func<string, HttpResponseMessage> responder) => _responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(_responder(request.RequestUri!.ToString()));
    }
}
