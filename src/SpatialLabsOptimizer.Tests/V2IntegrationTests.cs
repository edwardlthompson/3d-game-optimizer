using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Privacy;
using SpatialLabsOptimizer.Infrastructure.Updates;

namespace SpatialLabsOptimizer.Tests;

public class V2IntegrationTests
{
    [Fact]
    public void EpicGogScanner_ReturnsEmpty_WhenNotInstalled()
    {
        var scanner = new EpicGogLibraryScanner(
            epicManifestsPath: Path.Combine(Path.GetTempPath(), $"3dgo-no-epic-{Guid.NewGuid()}"),
            gogGamesPath: Path.Combine(Path.GetTempPath(), $"3dgo-no-gog-{Guid.NewGuid()}"));
        Assert.Empty(scanner.ScanEpicInstalledGames());
        Assert.Empty(scanner.ScanGogInstalledGames());
    }

    [Fact]
    public void EpicScanner_ParseManifest_UsesCatalogItemId()
    {
        var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"3dgo-epic-{Guid.NewGuid()}"));
        var manifestPath = Path.Combine(dir.FullName, "SampleGame.item");
        File.WriteAllText(manifestPath, """
            {
              "CatalogItemId": "epic-catalog-abc123",
              "DisplayName": "Sample Epic Game"
            }
            """);

        Assert.True(EpicGogLibraryScanner.TryParseEpicManifest(manifestPath, out var game));
        Assert.NotNull(game);
        Assert.Equal("Epic", game!.Store);
        Assert.Equal("epic-catalog-abc123", game.ExternalId);
        Assert.Equal("Sample Epic Game", game.Title);
        Assert.Equal(ExternalStoreIdMapper.StableAppId("Epic", "epic-catalog-abc123"), game.StableAppId);
    }

    [Fact]
    public void GogScanner_ParseInfo_UsesProductId()
    {
        var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"3dgo-gog-{Guid.NewGuid()}"));
        var gameDir = Directory.CreateDirectory(Path.Combine(dir.FullName, "Cyberpunk"));
        var infoPath = Path.Combine(gameDir.FullName, "goggame-1207659029.info");
        File.WriteAllText(infoPath, """{"gameId":1207659029,"name":"Cyberpunk GOG"}""");

        Assert.True(EpicGogLibraryScanner.TryParseGogInfoFile(infoPath, out var game));
        Assert.NotNull(game);
        Assert.Equal("GOG", game!.Store);
        Assert.Equal("1207659029", game.ExternalId);
        Assert.Equal("Cyberpunk GOG", game.Title);
    }

    [Fact]
    public async Task WorkshopImporter_RejectsDisallowedHost()
    {
        var dataRoot = TestPaths.FindDataRoot();
        var loader = new JsonDataLoader(dataRoot);
        var hub = new Infrastructure.Progress.OperationProgressHub();
        var handler = new PrivacyGuardHttpHandler(new PrivacyGuard(PrivacyAllowlist.DefaultHosts))
        {
            InnerHandler = new StubMessageHandler()
        };
        var gateway = new ExternalDataGateway(handler, hub);
        var presets = new PresetCacheService(loader, gateway);
        var importer = new WorkshopPresetImporter(loader, presets, gateway, new PrivacyGuard(PrivacyAllowlist.DefaultHosts));

        var count = await importer.ImportFromUrlAsync("https://example.com/presets.json");
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task WorkshopImporter_ImportsAllowlistedSourceManifest()
    {
        var dataRoot = TestPaths.FindDataRoot();
        var loader = new JsonDataLoader(dataRoot);
        var hub = new Infrastructure.Progress.OperationProgressHub();
        var handler = new PrivacyGuardHttpHandler(new PrivacyGuard(PrivacyAllowlist.DefaultHosts))
        {
            InnerHandler = new StubMessageHandler()
        };
        var gateway = new ExternalDataGateway(handler, hub);
        var presets = new PresetCacheService(loader, gateway);
        var importer = new WorkshopPresetImporter(loader, presets, gateway, new PrivacyGuard(PrivacyAllowlist.DefaultHosts));

        var count = await importer.ImportAllowlistedSourcesAsync();
        Assert.True(count >= 4);
    }

    [Fact]
    public async Task LanPartyExport_WritesTitlePayloadWithoutPii()
    {
        var service = new LanPartyExportService();
        var path = await service.ExportSessionAsync([
            new LanPartyExportService.ExportEntry(570, "Dota 2"),
            new LanPartyExportService.ExportEntry(1091500, "Cyberpunk 2077")
        ]);

        Assert.True(File.Exists(path));
        var json = await File.ReadAllTextAsync(path);
        Assert.Contains("Dota 2", json);
        Assert.Contains("1091500", json);
        Assert.DoesNotContain("@", json);
    }

    [Fact]
    public async Task HybridSession_PersistsSessionCode()
    {
        var path = Path.Combine(Path.GetTempPath(), $"3dgo-hybrid-{Guid.NewGuid()}.db");
        await using var store = new SqliteSettingsStore(path);
        await store.InitializeAsync();
        var hybrid = new HybridSessionService(store, new ThreeDGoCodeService());

        var session = await hybrid.CreateSessionAsync(1091500);
        var loaded = await hybrid.GetActiveSessionAsync();

        Assert.StartsWith("3DGO-", session.SessionCode);
        Assert.NotNull(loaded);
        Assert.Equal(session.SessionCode, loaded!.SessionCode);
        Assert.Equal(1091500, loaded.HostAppId);
    }

    [Fact]
    public async Task MultiStoreMerge_UsesParsedExternalTitles()
    {
        var epicDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"3dgo-merge-epic-{Guid.NewGuid()}"));
        File.WriteAllText(Path.Combine(epicDir.FullName, "Fortnite.item"), """
            {"CatalogItemId":"epic-fortnite-001","DisplayName":"Fortnite Epic"}
            """);

        var dbPath = Path.Combine(Path.GetTempPath(), $"3dgo-merge-{Guid.NewGuid()}.db");
        await using var db = new GameDatabase(dbPath);
        await db.InitializeAsync();

        var scanner = new EpicGogLibraryScanner(epicDir.FullName, Path.Combine(Path.GetTempPath(), $"3dgo-merge-gog-{Guid.NewGuid()}"));
        var epicGames = scanner.ScanEpicInstalledGames();
        Assert.Single(epicGames);

        var game = epicGames[0];
        await db.UpsertGameAsync(new Domain.GameCatalogItem(
            game.StableAppId,
            $"{game.Store}: {game.Title}",
            Domain.CompatibilityTier.Experimental,
            Domain.LaunchReadinessState.NeedsPresetCache,
            true,
            null, null, null, null, null, game.Store, false));

        var loaded = await db.GetGameAsync(game.StableAppId);
        Assert.NotNull(loaded);
        Assert.Contains("Fortnite Epic", loaded!.Title);
    }
}
