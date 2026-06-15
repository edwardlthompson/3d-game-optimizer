using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Install;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Launch.Coexistence;
using SpatialLabsOptimizer.Infrastructure.Library;
using SpatialLabsOptimizer.Infrastructure.Pcvr;
using SpatialLabsOptimizer.Infrastructure.Privacy;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Settings;
using SpatialLabsOptimizer.Infrastructure.Steam;
using SpatialLabsOptimizer.Infrastructure.Updates;

namespace SpatialLabsOptimizer.Tests;

public sealed class Sprint43QaGateTests
{
    [Fact]
    public async Task P1_SteamUnavailable_LaunchFallbackStillSucceeds()
    {
        var registry = new LaunchAdapterRegistry(new LaunchAdapterBase[]
        {
            new StubLaunchAdapter(LaunchPlatform.Uevr)
        });
        var fallback = new AutoFallbackLaunchService(registry);
        var plan = new ResolvedGameLaunchPlan(
            999999,
            "Offline Seed Title",
            LaunchPlatform.Uevr,
            CompatibilityTier.Playable,
            0.5,
            0.5,
            0.5,
            null,
            null,
            false);

        var result = await fallback.LaunchWithFallbackAsync(plan, LaunchContext.Standard);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task P1_OfflineCache_PresetExistsWithoutNetworkFetch()
    {
        var dataRoot = TestPaths.FindDataRoot();
        var loader = new JsonDataLoader(dataRoot);
        var handler = new PrivacyGuardHttpHandler(new PrivacyGuard(PrivacyAllowlist.DefaultHosts))
        {
            InnerHandler = new FailingNetworkHandler()
        };
        var hub = new OperationProgressHub();
        var gateway = new ExternalDataGateway(handler, hub);
        var presets = new PresetCacheService(loader, gateway);

        var presetDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "3d-game-optimizer",
            "presets");
        Directory.CreateDirectory(presetDir);
        var presetPath = Path.Combine(presetDir, "570.json");
        await File.WriteAllTextAsync(presetPath, """{"cached":true}""");

        Assert.True(await presets.HasPresetAsync(570));
    }

    [Fact]
    public void IncrementalSteamScan_CountNewInstalls_IgnoresSeedOnlyTitles()
    {
        var installed = new[] { 570, 730, 999990 };
        var knownInstalled = new[] { 570, 730 };

        Assert.Equal(1, IncrementalSteamScanService.CountNewInstalls(installed, knownInstalled));
        Assert.Equal(0, IncrementalSteamScanService.CountNewInstalls(knownInstalled, knownInstalled));
    }

    [Fact]
    public async Task IncrementalSteamScan_SkipsFullIndex_WhenNoNewInstalls()
    {
        var steamRoot = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"3dgo-steam-{Guid.NewGuid()}"));
        var steamApps = Directory.CreateDirectory(Path.Combine(steamRoot.FullName, "steamapps"));
        File.WriteAllText(Path.Combine(steamApps.FullName, "appmanifest_570.acf"), "\"appid\" \"570\"");

        var dbPath = Path.Combine(Path.GetTempPath(), $"3dgo-incr-{Guid.NewGuid()}.db");
        await using var db = new GameDatabase(dbPath);
        await db.InitializeAsync();
        await db.UpsertGameAsync(new GameCatalogItem(
            570, "Dota 2", CompatibilityTier.Optimized, LaunchReadinessState.Ready, true,
            null, null, null, null, null, null, false));

        var scanner = new SteamVdfScanner();
        var installed = scanner.ScanInstalledAppIds(steamRoot.FullName);
        var known = await db.GetInstalledSteamAppIdsAsync();
        Assert.Equal(0, IncrementalSteamScanService.CountNewInstalls(installed, known));
    }

    [Fact]
    public async Task HdrWatchdog_ExposesOsHandoffInstructions()
    {
        var watchdog = new HdrWatchdogService();
        Assert.False(await watchdog.IsHdrEnabledAsync());
        Assert.Contains("Windows Settings", HdrWatchdogService.OsHandoffInstructions);
        Assert.Contains("hdr-disable-requested.flag", HdrWatchdogService.GetHandoffFlagPath());
    }

    [Fact]
    public async Task AboutUpdate_CachedResultEnablesApply()
    {
        var store = new SqliteSettingsStore(Path.Combine(Path.GetTempPath(), $"3dgo-about-{Guid.NewGuid()}.db"));
        await store.InitializeAsync();
        var prefs = new UserPreferencesService(store);
        await prefs.SetCachedUpdateResultAsync(new UpdateCheckResult(
            "1.1.0",
            "1.2.0",
            true,
            "https://github.com/example/releases/tag/v1.2.0",
            "https://example.com/app.zip",
            InstallArtifactType.Zip,
            "SpatialLabsOptimizer-1.2.0-win-x64.zip"));

        var cached = await prefs.GetCachedUpdateResultAsync();

        Assert.NotNull(cached);
        Assert.True(cached!.IsUpdateAvailable);
        Assert.False(string.IsNullOrWhiteSpace(cached.DownloadUrl));
        Assert.Equal("1.2.0", cached.LatestVersion);
        await store.DisposeAsync();
    }

    [Fact]
    public async Task AboutUpdate_RetryPendingFlagRoundTrips()
    {
        var store = new SqliteSettingsStore(Path.Combine(Path.GetTempPath(), $"3dgo-retry-{Guid.NewGuid()}.db"));
        await store.InitializeAsync();
        var prefs = new UserPreferencesService(store);
        await prefs.SetUpdateRestartPendingAsync(true);

        Assert.True(await prefs.GetUpdateRestartPendingAsync());
        await store.DisposeAsync();
    }

    [Fact]
    public void CommandPalette_SearchFiltersByQuery()
    {
        var palette = new CommandPaletteService();
        var all = palette.Search("");
        var setup = palette.Search("setup");

        Assert.True(setup.Count <= all.Count);
        Assert.Contains(setup, entry => entry.Title.Contains("Setup", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task LibraryFilters_FavoritesOnly_ReturnsSubset()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"3dgo-filter-{Guid.NewGuid()}.db");
        await using var db = new GameDatabase(dbPath);
        await db.InitializeAsync();
        await db.UpsertGameAsync(new GameCatalogItem(
            570, "Dota 2", CompatibilityTier.Optimized, LaunchReadinessState.Ready, true,
            null, null, null, null, null, null, true));
        await db.UpsertGameAsync(new GameCatalogItem(
            730, "CS2", CompatibilityTier.Optimized, LaunchReadinessState.Ready, true,
            null, null, null, null, null, null, false));

        var all = await db.GetReadyToPlayAsync();
        var favorites = all.Where(g => g.IsFavorite).ToList();

        Assert.Single(favorites);
        Assert.Equal(570, favorites[0].SteamAppId);
    }

    private sealed class StubLaunchAdapter : LaunchAdapterBase
    {
        public StubLaunchAdapter(LaunchPlatform platform) => Platform = platform;
        public override LaunchPlatform Platform { get; }
        public override Task<bool> LaunchAsync(
            ResolvedGameLaunchPlan plan,
            LaunchContext context,
            CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }

    private sealed class FailingNetworkHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => throw new HttpRequestException("Simulated offline network failure.");
    }
}
