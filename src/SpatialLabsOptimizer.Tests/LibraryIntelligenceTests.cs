using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Library;

namespace SpatialLabsOptimizer.Tests;

public class LibraryIntelligenceTests
{
    [Fact]
    public void WhyNotReadyFilter_ReturnsNonReadyTitles()
    {
        var service = CreateService(new GameDatabase(Path.Combine(Path.GetTempPath(), $"3dgo-intel-{Guid.NewGuid()}.db")));
        var games = new List<GameCatalogItem>
        {
            new(1, "Ready", CompatibilityTier.Optimized, LaunchReadinessState.Ready, true, null, null, null, null, null, null, false),
            new(2, "Needs preset", CompatibilityTier.Playable, LaunchReadinessState.NeedsPresetCache, true, null, null, null, null, null, null, false),
            new(3, "Blocked", CompatibilityTier.Unsupported, LaunchReadinessState.Blocked, true, null, null, null, null, null, null, false)
        };

        var filtered = service.ApplyWhyNotReadyFilter(games);
        Assert.Equal(2, filtered.Count);
        Assert.DoesNotContain(filtered, g => g.Readiness == LaunchReadinessState.Ready);
    }

    [Fact]
    public void SmartCollection_LocalOnly_FiltersDescriptor()
    {
        var service = CreateService(new GameDatabase(Path.Combine(Path.GetTempPath(), $"3dgo-intel-local-{Guid.NewGuid()}.db")));
        var games = new List<GameCatalogItem>
        {
            new(1, "Steam", CompatibilityTier.Optimized, LaunchReadinessState.Ready, true, null, null, null, null, null, "RPG", false),
            new(2, "Offline", CompatibilityTier.Experimental, LaunchReadinessState.NeedsPresetCache, true, null, null, null, null, null, "Local", false)
        };

        var filtered = service.ApplySmartCollection(games, SmartCollectionMode.LocalOnly);
        Assert.Single(filtered);
        Assert.Equal("Local", filtered[0].ReviewDescriptor);
    }

    [Fact]
    public void CompatibilityBadge_LocalAndVerified()
    {
        Assert.Equal("Local", LibraryIntelligenceService.GetCompatibilityBadge(
            CompatibilityTier.Experimental, LaunchReadinessState.NeedsPresetCache, isLocal: true));
        Assert.Equal("Verified", LibraryIntelligenceService.GetCompatibilityBadge(
            CompatibilityTier.Optimized, LaunchReadinessState.Ready, isLocal: false));
    }

    [Fact]
    public async Task RecentLaunches_PersistInSqlite()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"3dgo-recent-{Guid.NewGuid()}.db");
        await using var db = new GameDatabase(dbPath);
        await db.InitializeAsync();
        var service = CreateService(db);

        await service.RecordLaunchAsync(570, "Dota 2", true);
        await service.RecordLaunchAsync(1091500, "Cyberpunk", false, "3DGO-0005");

        var recent = await service.GetRecentLaunchesAsync();
        Assert.Equal(2, recent.Count);
        Assert.Contains(recent, r => r.Title == "Dota 2" && r.Success);
        Assert.Contains(recent, r => r.Title == "Cyberpunk" && r.ErrorCode == "3DGO-0005");
    }

    [Fact]
    public async Task CompatibilityNotes_RoundTrip()
    {
        var settingsPath = Path.Combine(Path.GetTempPath(), $"3dgo-notes-{Guid.NewGuid()}.db");
        await using var store = new SqliteSettingsStore(settingsPath);
        await store.InitializeAsync();
        var dbPath = Path.Combine(Path.GetTempPath(), $"3dgo-notes-games-{Guid.NewGuid()}.db");
        await using var db = new GameDatabase(dbPath);
        await db.InitializeAsync();
        var service = CreateService(db, store);

        await service.SaveCompatibilityNoteAsync(570, "Works best at depth 0.7");
        var note = await service.GetCompatibilityNoteAsync(570);

        Assert.Equal("Works best at depth 0.7", note);
    }

    [Fact]
    public async Task PresetFreshnessIndicator_ReportsStaleWhenOld()
    {
        var dataRoot = TestPaths.FindDataRoot();
        var loader = new JsonDataLoader(dataRoot);
        var hub = new Infrastructure.Progress.OperationProgressHub();
        var gateway = new ExternalDataGateway(
            new Infrastructure.Privacy.PrivacyGuardHttpHandler(
                new Infrastructure.Privacy.PrivacyGuard(Infrastructure.Privacy.PrivacyAllowlist.DefaultHosts))
            {
                InnerHandler = new StubMessageHandler()
            },
            hub);
        var presets = new PresetCacheService(loader, gateway);
        var dbPath = Path.Combine(Path.GetTempPath(), $"3dgo-fresh-{Guid.NewGuid()}.db");
        await using var db = new GameDatabase(dbPath);
        await db.InitializeAsync();
        var settingsPath = Path.Combine(Path.GetTempPath(), $"3dgo-fresh-settings-{Guid.NewGuid()}.db");
        await using var store = new SqliteSettingsStore(settingsPath);
        await store.InitializeAsync();
        var service = new LibraryIntelligenceService(db, presets, new CompatibilityNotesRepository(store));

        var appId = 999001;
        var presetDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "3d-game-optimizer",
            "presets");
        Directory.CreateDirectory(presetDir);
        var presetPath = Path.Combine(presetDir, $"{appId}.json");
        await File.WriteAllTextAsync(presetPath, "{}");
        File.SetLastWriteTimeUtc(presetPath, DateTime.UtcNow.AddDays(-45));

        var label = await service.GetPresetFreshnessLabelAsync(appId);
        Assert.Equal("Preset stale (>30d)", label);
    }

    private static LibraryIntelligenceService CreateService(GameDatabase db, SqliteSettingsStore? store = null)
    {
        store ??= new SqliteSettingsStore(Path.Combine(Path.GetTempPath(), $"3dgo-intel-settings-{Guid.NewGuid()}.db"));
        var dataRoot = TestPaths.FindDataRoot();
        var loader = new JsonDataLoader(dataRoot);
        var hub = new Infrastructure.Progress.OperationProgressHub();
        var gateway = new ExternalDataGateway(
            new Infrastructure.Privacy.PrivacyGuardHttpHandler(
                new Infrastructure.Privacy.PrivacyGuard(Infrastructure.Privacy.PrivacyAllowlist.DefaultHosts))
            {
                InnerHandler = new StubMessageHandler()
            },
            hub);
        var presets = new PresetCacheService(loader, gateway);
        return new LibraryIntelligenceService(db, presets, new CompatibilityNotesRepository(store));
    }

    private sealed class StubMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(Array.Empty<byte>())
            });
    }
}
