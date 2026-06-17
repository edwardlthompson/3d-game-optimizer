using SpatialLabsOptimizer.Application.UseCases;
using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Compatibility;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Install;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Launch.Coexistence;
using SpatialLabsOptimizer.Infrastructure.Library;
using SpatialLabsOptimizer.Infrastructure.Pcvr;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Settings;
using SpatialLabsOptimizer.Infrastructure.Updates;

namespace SpatialLabsOptimizer.Tests;

public class PlayIn3DLaunchTests
{
    [Fact]
    public async Task PlayIn3D_PublishesRealProgressSteps_OnSuccessfulLaunch()
    {
        var dataRoot = TestPaths.FindDataRoot();
        var reports = new List<OperationProgressReport>();
        var hub = new OperationProgressHub();
        hub.ProgressPublished += (_, report) => reports.Add(report);

        var playIn3D = BuildPlayIn3D(hub, succeedLaunch: true);
        var (success, error) = await playIn3D.ExecuteAsync(1091500);

        Assert.True(success, error);
        Assert.Null(error);
        Assert.True(reports.Count >= 8);
        Assert.Contains(reports, r => r.CurrentStep.Contains("readiness", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(reports, r => r.CurrentStep.Contains("preset", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(reports, r => r.CurrentStep.Contains("Resolving game settings", StringComparison.Ordinal) && r.StepIndex == 8);
        Assert.All(reports.Where(r => r.PercentComplete.HasValue), r => Assert.InRange(r.PercentComplete!.Value, 0, 100));
    }

    [Fact]
    public async Task PlayIn3D_RollbackSnapshot_WhenLaunchFails()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"3dgo-play3d-{Guid.NewGuid()}.db");
        var store = new SqliteSettingsStore(dbPath);
        await store.InitializeAsync();
        var overrides = new GameOverrideRepository(store);
        await overrides.SaveAsync(new GameOverride(570, 0.55, 0.45, LaunchPlatform.Uevr, false, "Monitor"));

        var hub = new OperationProgressHub();
        var playIn3D = BuildPlayIn3D(hub, succeedLaunch: false, overrides);

        var (success, error) = await playIn3D.ExecuteAsync(570);

        Assert.False(success);
        Assert.Equal("3DGO-0005", error);
        var restored = await overrides.GetAsync(570);
        Assert.NotNull(restored);
        Assert.Equal(0.55, restored!.Depth);
        Assert.Equal("Monitor", restored.PreferredOutput);

        await store.DisposeAsync();
    }

    [Fact]
    public async Task LaunchDisplayHandoffService_ReturnsPersistedTarget()
    {
        var path = Path.Combine(Path.GetTempPath(), $"3dgo-display-handoff-{Guid.NewGuid()}.db");
        await using var store = new SqliteSettingsStore(path);
        await store.InitializeAsync();
        var prefs = new UserPreferencesService(store);
        var probe = new FakeDisplayEdidProbe(() =>
        [
            new DisplayEdidSnapshot("panel-a", "sig-a", "3D Panel"),
            new DisplayEdidSnapshot("panel-b", "sig-b", "Secondary")
        ]);
        var picker = new MultiMonitorLaunchPicker(probe, prefs);
        await picker.SetSelectedTargetAsync("panel-b");
        var handoff = new LaunchDisplayHandoffService(picker);

        var target = await handoff.PrepareAsync();

        Assert.NotNull(target);
        Assert.Equal("panel-b", target!.DeviceId);
        Assert.Contains("Secondary", LaunchDisplayHandoffService.FormatHandoffMessage(target), StringComparison.Ordinal);
    }

    private static PlayIn3D BuildPlayIn3D(
        OperationProgressHub hub,
        bool succeedLaunch,
        GameOverrideRepository? overrides = null)
    {
        var dataRoot = TestPaths.FindDataRoot();
        var loader = new JsonDataLoader(dataRoot);
        var compat = new CompatibilityRepository(loader);
        var detector = TestPaths.CreateDisplayAutoDetector();
        var handler = new Infrastructure.Privacy.PrivacyGuardHttpHandler(
            new Infrastructure.Privacy.PrivacyGuard(Infrastructure.Privacy.PrivacyAllowlist.DefaultHosts))
        {
            InnerHandler = new StubMessageHandler()
        };
        var gateway = new ExternalDataGateway(handler, hub);
        var presets = new PresetCacheService(loader, gateway);
        var readiness = new LaunchReadinessService(presets);
        var coexistence = new ExternalToolCoexistenceService(new FakeRunningProcessProbe([]));
        var installPaths = new GameInstallPathResolver();
        var launcher = new ProcessLauncher(installPaths);
        var configWriter = new ToolConfigWriter();
        var defaults = new OptimalDefaultsService(loader, configWriter);
        var dbPath = Path.Combine(Path.GetTempPath(), $"3dgo-play3d-db-{Guid.NewGuid()}.db");
        var store = new SqliteSettingsStore(dbPath);
        store.InitializeAsync().GetAwaiter().GetResult();
        var overrideRepo = overrides ?? new GameOverrideRepository(store);
        var snapshots = new ConfigSnapshotService(overrideRepo);
        var router = new LaunchPlatformRouter();
        var resolve = new ResolveGameSettings(defaults, overrideRepo, compat, router);
        var registry = new LaunchAdapterRegistry(new LaunchAdapterBase[]
        {
            new StubLaunchAdapter(LaunchPlatform.Uevr, succeedLaunch),
            new StubLaunchAdapter(LaunchPlatform.ReShade, succeedLaunch),
            new StubLaunchAdapter(LaunchPlatform.TrueGame, succeedLaunch),
            new StubLaunchAdapter(LaunchPlatform.Tweak, succeedLaunch),
            new StubLaunchAdapter(LaunchPlatform.Odyssey3DHub, succeedLaunch),
            new StubLaunchAdapter(LaunchPlatform.Nvidia3DVision, succeedLaunch)
        });
        var fallback = new AutoFallbackLaunchService(registry);
        var probe = new FakeDisplayEdidProbe(() =>
        [
            new DisplayEdidSnapshot("panel-a", "sig-a", "3D Panel")
        ]);
        var prefs = new UserPreferencesService(store);
        var displayHandoff = new LaunchDisplayHandoffService(new MultiMonitorLaunchPicker(probe, prefs));
        var gameDb = new GameDatabase(dbPath);
        gameDb.InitializeAsync().GetAwaiter().GetResult();
        var intelligence = new LibraryIntelligenceService(
            gameDb,
            presets,
            new CompatibilityNotesRepository(store));
        var pcvr = new PlayInVR(new PcvrRuntimeConnector(prefs), compat, hub);

        return new PlayIn3D(
            resolve,
            detector,
            presets,
            readiness,
            coexistence,
            new GameFirstLaunchOrchestrator(installPaths, launcher, new FakeRunningProcessProbe([])),
            configWriter,
            snapshots,
            new LaunchErrorCatalog(),
            hub,
            new LaunchAuditService(),
            fallback,
            pcvr,
            new SafeLaunchService(launcher),
            prefs,
            new LaunchPreviewService(),
            intelligence,
            gameDb,
            defaults,
            displayHandoff,
            installPaths);
    }

    private sealed class FakeRunningProcessProbe : IRunningProcessProbe
    {
        private readonly HashSet<string> _running;

        public FakeRunningProcessProbe(IEnumerable<string> running) =>
            _running = new HashSet<string>(running, StringComparer.OrdinalIgnoreCase);

        public bool IsProcessRunning(string processName) => _running.Contains(processName);

        public IReadOnlyList<string> GetRunningFrom(params string[] processNames) =>
            processNames.Where(IsProcessRunning).ToList();
    }

    private sealed class StubLaunchAdapter : LaunchAdapterBase
    {
        private readonly bool _succeed;

        public StubLaunchAdapter(LaunchPlatform platform, bool succeed)
        {
            Platform = platform;
            _succeed = succeed;
        }

        public override LaunchPlatform Platform { get; }

        public override Task<bool> LaunchAsync(
            ResolvedGameLaunchPlan plan,
            LaunchContext context,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_succeed);
    }
}
