using SpatialLabsOptimizer.Application.Progress;
using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Compatibility;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Launch.Coexistence;
using SpatialLabsOptimizer.Infrastructure.Library;
using SpatialLabsOptimizer.Infrastructure.Privacy;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Steam;

namespace SpatialLabsOptimizer.Tests;

public class InfrastructureTests
{
    [Fact]
    public async Task PrivacyGuardHttpHandler_BlocksNonAllowlistedHost()
    {
        var guard = new PrivacyGuard(PrivacyAllowlist.DefaultHosts);
        using var handler = new PrivacyGuardHttpHandler(guard)
        {
            InnerHandler = new StubMessageHandler()
        };
        using var client = new HttpClient(handler);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.GetAsync("https://example.com"));
    }

    [Fact]
    public async Task PrivacyGuardHttpHandler_AllowsSteamStore()
    {
        var guard = new PrivacyGuard(PrivacyAllowlist.DefaultHosts);
        using var handler = new PrivacyGuardHttpHandler(guard)
        {
            InnerHandler = new StubMessageHandler()
        };
        using var client = new HttpClient(handler);

        var response = await client.GetAsync("https://store.steampowered.com/api/appdetails?appids=570");
        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public void OperationProgressHub_PublishesProgressEvents()
    {
        var hub = new OperationProgressHub();
        OperationProgressReport? observed = null;
        hub.ProgressPublished += (_, report) => observed = report;

        var expected = new OperationProgressReport(
            "op-123",
            OperationCategory.Launch,
            "Play in 3D",
            "Starting game…",
            PercentComplete: 42);

        hub.Publish(expected);

        Assert.NotNull(observed);
        Assert.Equal(expected.OperationId, observed!.OperationId);
        Assert.Equal(expected.PercentComplete, observed.PercentComplete);
    }

    [Fact]
    public void WilsonScore_RanksHighVolumeAbovePerfectLowVolume()
    {
        var lowVolume = WilsonScoreCalculator.Compute(100, 10);
        var highVolume = WilsonScoreCalculator.Compute(94, 10_000);
        Assert.True(highVolume > lowVolume);
    }

    [Fact]
    public void LaunchPlatformRouter_UsesVendorPreferredPlatform()
    {
        var router = new LaunchPlatformRouter();
        var profile = new DisplayProfile("acer-psv27-2", "Acer", "PSV27-2", "View 27", "monitor", [], "profile", []);
        var adapter = new AcerSpatialLabsAdapter(profile);

        var platform = router.Route(CompatibilityTier.Optimized, adapter);
        Assert.Equal(LaunchPlatform.TrueGame, platform);
    }

    [Fact]
    public async Task CompatibilityRepository_LoadsSeedGames()
    {
        var dataRoot = FindDataRoot();
        var repo = new CompatibilityRepository(new JsonDataLoader(dataRoot));
        var games = await repo.GetAllAsync();
        Assert.True(games.Count >= 3);
        Assert.Contains(games, g => g.SteamAppId == 1091500);
    }

    [Fact]
    public void LaunchErrorCatalog_ReturnsKnownCodes()
    {
        var catalog = new LaunchErrorCatalog();
        var (message, recovery) = catalog.Get("3DGO-0001");
        Assert.Contains("Preset", message);
        Assert.False(string.IsNullOrWhiteSpace(recovery));
    }

    [Fact]
    public async Task SqliteSettingsStore_PersistsDisclaimerFlag()
    {
        var path = Path.Combine(Path.GetTempPath(), $"3dgo-test-{Guid.NewGuid()}.db");
        var store = new SqliteSettingsStore(path);
        await store.InitializeAsync();
        await store.SetDisclaimerAcceptedAsync(true);
        Assert.True(await store.GetDisclaimerAcceptedAsync());
        await store.DisposeAsync();
    }

    [Fact]
    public void LibrarySortService_SortsByWilsonScore()
    {
        var service = new LibrarySortService();
        var games = new List<Domain.GameCatalogItem>
        {
            new(1, "A", CompatibilityTier.Playable, LaunchReadinessState.Ready, true, 100, 100, 10, 0.72, null, null, false),
            new(2, "B", CompatibilityTier.Playable, LaunchReadinessState.Ready, true, 100, 94, 10000, 0.938, null, null, false)
        };
        var sorted = service.Sort(games, LibrarySortMode.SteamReviews);
        Assert.Equal(2, sorted[0].SteamAppId);
    }

    [Fact]
    public async Task AutoFallbackLaunchService_FallsBackFromReShadeToUevr()
    {
        var registry = new LaunchAdapterRegistry(new LaunchAdapterBase[]
        {
            new FailingLaunchAdapter(LaunchPlatform.ReShade),
            new SucceedingLaunchAdapter(LaunchPlatform.Uevr)
        });
        var fallback = new AutoFallbackLaunchService(registry);
        var plan = new ResolvedGameLaunchPlan(570, "Dota 2", LaunchPlatform.ReShade, CompatibilityTier.Playable, 0.5, 0.5, 0.5, null, null, false);
        var result = await fallback.LaunchWithFallbackAsync(plan, LaunchContext.Standard);
        Assert.True(result.Success);
        Assert.Equal(LaunchPlatform.Uevr, result.UsedPlatform);
    }

    [Fact]
    public async Task LaunchAuditService_WritesLogEntry()
    {
        var audit = new LaunchAuditService();
        await audit.LogAsync(570, "Dota 2", LaunchPlatform.Uevr, true);
        Assert.EndsWith("launch-audit.log", audit.LogPath);
        Assert.True(File.Exists(audit.LogPath));
    }

    private static string FindDataRoot()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "data");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("data folder not found");
    }

    private sealed class FailingLaunchAdapter : LaunchAdapterBase
    {
        public FailingLaunchAdapter(LaunchPlatform platform) => Platform = platform;
        public override LaunchPlatform Platform { get; }
        public override Task<bool> LaunchAsync(
            ResolvedGameLaunchPlan plan,
            LaunchContext context,
            CancellationToken cancellationToken = default)
            => Task.FromResult(false);
    }

    private sealed class SucceedingLaunchAdapter : LaunchAdapterBase
    {
        public SucceedingLaunchAdapter(LaunchPlatform platform) => Platform = platform;
        public override LaunchPlatform Platform { get; }
        public override Task<bool> LaunchAsync(
            ResolvedGameLaunchPlan plan,
            LaunchContext context,
            CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }

    private sealed class StubMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}
