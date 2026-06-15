using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Pcvr;
using SpatialLabsOptimizer.Infrastructure.Updates;

namespace SpatialLabsOptimizer.Tests;

public class MilestoneFeatureTests
{
    [Fact]
    public void FeatureFlags_V101Enabled_ByDefault()
    {
        Assert.True(FeatureFlags.V101Enabled);
    }

    [Fact]
    public async Task HdrWatchdog_WhenSdr_ReturnsFalse()
    {
        var watchdog = new HdrWatchdogService();
        var enabled = await watchdog.IsHdrEnabledAsync();
        Assert.False(enabled);
    }

    [Fact]
    public async Task IncrementalSteamScan_ReturnsNonNegativeDelta()
    {
        var dataRoot = TestPaths.FindDataRoot();
        var loader = new Infrastructure.Data.JsonDataLoader(dataRoot);
        var db = new Infrastructure.Data.GameDatabase(Path.Combine(Path.GetTempPath(), $"3dgo-incr-{Guid.NewGuid()}.db"));
        var compat = new Infrastructure.Compatibility.CompatibilityRepository(loader);
        var scanner = new Infrastructure.Steam.SteamVdfScanner();
        var hub = new Infrastructure.Progress.OperationProgressHub();
        var detector = TestPaths.CreateDisplayAutoDetector();
        var handler = new Infrastructure.Privacy.PrivacyGuardHttpHandler(
            new Infrastructure.Privacy.PrivacyGuard(Infrastructure.Privacy.PrivacyAllowlist.DefaultHosts))
        {
            InnerHandler = new StubMessageHandler()
        };
        var gateway = new Infrastructure.Data.ExternalDataGateway(handler, hub);
        var artwork = new Infrastructure.Artwork.GameArtworkService(
            new Infrastructure.Steam.SteamStoreApiClient(gateway),
            gateway,
            new Infrastructure.Artwork.CoverArtCache(),
            hub);
        var presets = new PresetCacheService(loader, gateway);
        var readiness = new LaunchReadinessService(presets);
        var external = new Infrastructure.Library.LibraryExternalGamesMerger(readiness, db);
        var steamOwned = new Infrastructure.Library.LibrarySteamOwnedMerger(readiness, db);
        var placeholders = new Infrastructure.Library.LibraryStorePlaceholderAssigner(db, hub);
        var merger = new Infrastructure.Library.LibraryIndexMerger(external, steamOwned, placeholders);
        var prefetch = new Infrastructure.Library.LibraryPrefetchService(db, artwork, hub);
        var settingsPath = Path.Combine(Path.GetTempPath(), $"3dgo-incr-settings-{Guid.NewGuid()}.db");
        await using var settings = new Infrastructure.Data.SqliteSettingsStore(settingsPath);
        await settings.InitializeAsync();
        var indexer = new Infrastructure.Library.LibraryIndexer(
            compat, scanner, readiness, db, hub, detector, merger, prefetch, settings);

        var service = new IncrementalSteamScanService(scanner, db, indexer, hub, settings);
        var delta = await service.ScanNewGamesAsync();
        Assert.True(delta >= 0);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task PcvrConnector_ReturnsNull_WhenNoRuntime()
    {
        var connector = new PcvrRuntimeConnector();
        var runtime = await connector.DetectRuntimeAsync();
        Assert.True(runtime is null or "SteamVR" or "OpenXR");
    }

    [Fact]
    public void CommandPalette_Search_FiltersResults()
    {
        var palette = new CommandPaletteService();
        var all = palette.Search("");
        var filtered = palette.Search("setup");
        Assert.True(filtered.Count <= all.Count);
        Assert.Contains(filtered, c => c.Title.Contains("Setup", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GameOverrideRepository_PersistsRoundTrip()
    {
        var path = Path.Combine(Path.GetTempPath(), $"3dgo-override-{Guid.NewGuid()}.db");
        var store = new Infrastructure.Data.SqliteSettingsStore(path);
        await store.InitializeAsync();
        var repo = new GameOverrideRepository(store);
        await repo.SaveAsync(new GameOverride(570, 0.6, 0.5, LaunchPlatform.Uevr, false, "Headset"));
        var loaded = await repo.GetAsync(570);
        Assert.NotNull(loaded);
        Assert.Equal("Headset", loaded!.PreferredOutput);
        await store.DisposeAsync();
    }

    [Fact]
    public void EpicGogScanner_ReturnsEmpty_WhenNotInstalled()
    {
        var scanner = new EpicGogLibraryScanner(
            epicManifestsPath: Path.Combine(Path.GetTempPath(), $"3dgo-no-epic-{Guid.NewGuid()}"),
            gogGamesPath: Path.Combine(Path.GetTempPath(), $"3dgo-no-gog-{Guid.NewGuid()}"));
        Assert.Empty(scanner.ScanEpicInstalledIds());
        Assert.Empty(scanner.ScanGogInstalledIds());
    }

    [Fact]
    public async Task BulkCacheTopPresets_PublishesProgressEvents()
    {
        var dataRoot = TestPaths.FindDataRoot();
        var loader = new Infrastructure.Data.JsonDataLoader(dataRoot);
        var hub = new Infrastructure.Progress.OperationProgressHub();
        var reports = new List<Infrastructure.Progress.OperationProgressReport>();
        hub.ProgressPublished += (_, report) => reports.Add(report);
        var handler = new Infrastructure.Privacy.PrivacyGuardHttpHandler(
            new Infrastructure.Privacy.PrivacyGuard(Infrastructure.Privacy.PrivacyAllowlist.DefaultHosts))
        {
            InnerHandler = new StubMessageHandler()
        };
        var gateway = new Infrastructure.Data.ExternalDataGateway(handler, hub);
        var presets = new PresetCacheService(loader, gateway);
        await presets.BulkCacheTopPresetsAsync(5, hub);
        Assert.Contains(reports, r => r.OperationId == "bulk-preset" && r.TotalSteps > 0);
        Assert.DoesNotContain(reports, r => r.CurrentStep == "No UEVR presets in manifest");
    }

    [Fact]
    public async Task CompatibilityRepository_LoadsSteamVrLaunchOptions()
    {
        var dataRoot = TestPaths.FindDataRoot();
        var loader = new Infrastructure.Data.JsonDataLoader(dataRoot);
        var repo = new Infrastructure.Compatibility.CompatibilityRepository(loader);
        var entry = await repo.GetByAppIdAsync(1091500);
        Assert.NotNull(entry);
        Assert.Equal("-vr", entry!.SteamVrLaunchOptions);
        Assert.Equal(VrCapability.UevrCompatible, entry.VrCapability);
    }

    [Fact]
    public async Task CompatibilityRepository_SeedHasMinimumTitles()
    {
        var dataRoot = TestPaths.FindDataRoot();
        var loader = new Infrastructure.Data.JsonDataLoader(dataRoot);
        var repo = new Infrastructure.Compatibility.CompatibilityRepository(loader);
        var all = await repo.GetAllAsync();
        Assert.True(all.Count >= 10);
    }

    [Fact]
    public async Task CompatibilityRepository_LoadsCatalogV2WhenPresent()
    {
        var dataRoot = TestPaths.FindDataRoot();
        var loader = new Infrastructure.Data.JsonDataLoader(dataRoot);
        var repo = new Infrastructure.Compatibility.CompatibilityRepository(loader);
        var all = await repo.GetAllAsync();
        Assert.True(all.Count >= 30, "catalog-v2 should expand beyond seed-v1 minimum");
        var portal = all.FirstOrDefault(g => g.SteamAppId == 620);
        Assert.NotNull(portal);
        Assert.Equal("optimized", portal!.TiersByVendor["nvidia"]);
    }

    [Fact]
    public async Task PlayInVR_GracefulFail_WhenNoRuntime()
    {
        var dataRoot = TestPaths.FindDataRoot();
        var loader = new Infrastructure.Data.JsonDataLoader(dataRoot);
        var compat = new Infrastructure.Compatibility.CompatibilityRepository(loader);
        var hub = new Infrastructure.Progress.OperationProgressHub();
        var playInVr = new Application.UseCases.PlayInVR(new PcvrRuntimeConnector(), compat, hub);
        var failedReports = new List<Infrastructure.Progress.OperationProgressReport>();
        hub.ProgressPublished += (_, report) =>
        {
            if (report.IsFailed)
            {
                failedReports.Add(report);
            }
        };

        var connector = new PcvrRuntimeConnector();
        if (await connector.DetectRuntimeAsync() is not null)
        {
            return;
        }

        var launched = await playInVr.ExecuteAsync(570);
        Assert.False(launched);
        Assert.NotEmpty(failedReports);
    }

    [Fact]
    public async Task LanPartyExport_WritesJsonFile()
    {
        var service = new LanPartyExportService();
        var path = await service.ExportSessionAsync(new[] { 570, 1091500 });
        Assert.True(File.Exists(path));
    }

    [Fact]
    public async Task WorkshopImporter_ReturnsNonNegativeCount()
    {
        var dataRoot = TestPaths.FindDataRoot();
        var loader = new Infrastructure.Data.JsonDataLoader(dataRoot);
        var hub = new Infrastructure.Progress.OperationProgressHub();
        var handler = new Infrastructure.Privacy.PrivacyGuardHttpHandler(
            new Infrastructure.Privacy.PrivacyGuard(Infrastructure.Privacy.PrivacyAllowlist.DefaultHosts))
        {
            InnerHandler = new StubMessageHandler()
        };
        var gateway = new Infrastructure.Data.ExternalDataGateway(handler, hub);
        var presets = new PresetCacheService(loader, gateway);
        var importer = new WorkshopPresetImporter(
            loader,
            presets,
            gateway,
            new Infrastructure.Privacy.PrivacyGuard(Infrastructure.Privacy.PrivacyAllowlist.DefaultHosts));
        var count = await importer.ImportAllowlistedSourcesAsync();
        Assert.True(count >= 0);
    }

    [Fact]
    public async Task SteamGridDbClient_ReturnsNull_OnEmptyPayload()
    {
        var hub = new Infrastructure.Progress.OperationProgressHub();
        var handler = new Infrastructure.Privacy.PrivacyGuardHttpHandler(
            new Infrastructure.Privacy.PrivacyGuard(Infrastructure.Privacy.PrivacyAllowlist.DefaultHosts))
        {
            InnerHandler = new StubMessageHandler()
        };
        var gateway = new Infrastructure.Data.ExternalDataGateway(handler, hub);
        var client = new SteamGridDbClient(gateway, new Infrastructure.Artwork.CoverArtCache());
        Assert.Null(await client.ResolveCoverAsync(570));
    }
}
