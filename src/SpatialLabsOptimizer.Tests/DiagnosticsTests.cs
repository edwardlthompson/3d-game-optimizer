using SpatialLabsOptimizer.Infrastructure.Compatibility;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Install;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Launch.Coexistence;
using SpatialLabsOptimizer.Infrastructure.Pcvr;
using SpatialLabsOptimizer.Infrastructure.Performance;
using SpatialLabsOptimizer.Infrastructure.Privacy;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Settings;
using SpatialLabsOptimizer.Infrastructure.Updates;

namespace SpatialLabsOptimizer.Tests;

public class DiagnosticsTests
{
    [Fact]
    public void ProtocolRegistrationService_ParsesPlayUri()
    {
        Assert.True(ProtocolRegistrationService.TryParsePlayUri("3dgo://play/570", out var appId));
        Assert.Equal(570, appId);
        Assert.False(ProtocolRegistrationService.TryParsePlayUri("https://example.com", out _));
    }

    [Fact]
    public async Task LaunchDryRunService_ReturnsStepsWithoutLaunching()
    {
        var dataRoot = TestPaths.FindDataRoot();
        var loader = new JsonDataLoader(dataRoot);
        var hub = new OperationProgressHub();
        var handler = new PrivacyGuardHttpHandler(new PrivacyGuard(PrivacyAllowlist.DefaultHosts))
        {
            InnerHandler = new StubMessageHandler()
        };
        var gateway = new ExternalDataGateway(handler, hub);
        var presets = new PresetCacheService(loader, gateway);
        var defaults = new OptimalDefaultsService(loader, new ToolConfigWriter());
        var overrides = new GameOverrideRepository(new SqliteSettingsStore(Path.Combine(Path.GetTempPath(), $"3dgo-dry-{Guid.NewGuid()}.db")));
        var compat = new CompatibilityRepository(loader);
        var router = new LaunchPlatformRouter();
        var resolve = new ResolveGameSettings(defaults, overrides, compat, router);
        var detector = TestPaths.CreateDisplayAutoDetector();
        var coexistence = new ExternalToolCoexistenceService(new FakeRunningProcessProbe([]));
        var settingsPath = Path.Combine(Path.GetTempPath(), $"3dgo-dry-prefs-{Guid.NewGuid()}.db");
        await using var settings = new SqliteSettingsStore(settingsPath);
        await settings.InitializeAsync();
        var prefs = new UserPreferencesService(settings);
        var preview = new LaunchPreviewService();
        var errors = new LaunchErrorCatalog();
        var dryRun = new LaunchDryRunService(
            resolve,
            detector,
            presets,
            coexistence,
            prefs,
            preview,
            errors,
            hub);

        var result = await dryRun.SimulateAsync(1091500);

        Assert.NotEmpty(result.Steps);
        Assert.Contains(result.Steps, step => step.Contains("Dry run", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DiagnosticBundleService_IncludesStructuredArtifacts()
    {
        var dataRoot = TestPaths.FindDataRoot();
        var loader = new JsonDataLoader(dataRoot);
        var audit = new LaunchAuditService();
        var detector = TestPaths.CreateDisplayAutoDetector();
        var toolPaths = new ToolPathResolver(Path.Combine(Path.GetTempPath(), $"3dgo-tools-{Guid.NewGuid()}"));
        var settingsPath = Path.Combine(Path.GetTempPath(), $"3dgo-bundle-{Guid.NewGuid()}.db");
        await using var settings = new SqliteSettingsStore(settingsPath);
        await settings.InitializeAsync();
        var toolDetector = new ToolInstallDetector(loader, toolPaths, settings);
        var coexistence = new ExternalToolCoexistenceService(new FakeRunningProcessProbe([]));
        var prefs = new UserPreferencesService(settings);
        var detectorArtifact = new InstallArtifactDetector(new FakeMsiProbe(false));
        var service = new DiagnosticBundleService(
            audit,
            detector,
            loader,
            toolDetector,
            coexistence,
            prefs,
            detectorArtifact);

        var zipPath = await service.ExportAsync();
        Assert.True(File.Exists(zipPath));
    }

    [Fact]
    public async Task LanPresetExportService_WritesAllowlistedMetadataOnly()
    {
        var dataRoot = TestPaths.FindDataRoot();
        var loader = new JsonDataLoader(dataRoot);
        var service = new LanPresetExportService(loader, new PrivacyGuard(PrivacyAllowlist.DefaultHosts));
        var path = await service.ExportAllowlistedPresetsAsync([
            new LanPresetExportService.ExportEntry(1091500, "Cyberpunk 2077")
        ]);

        Assert.True(File.Exists(path));
        var json = await File.ReadAllTextAsync(path);
        Assert.Contains("allowlistOnly", json);
        Assert.DoesNotContain("@", json);
    }

    [Fact]
    public async Task SeedContributionExportService_RedactsOutput()
    {
        var dataRoot = TestPaths.FindDataRoot();
        var loader = new JsonDataLoader(dataRoot);
        var settingsPath = Path.Combine(Path.GetTempPath(), $"3dgo-seed-{Guid.NewGuid()}.db");
        await using var settings = new SqliteSettingsStore(settingsPath);
        await settings.InitializeAsync();
        var prefs = new UserPreferencesService(settings);
        var detector = TestPaths.CreateDisplayAutoDetector();
        var toolPaths = new ToolPathResolver(Path.Combine(Path.GetTempPath(), $"3dgo-seed-tools-{Guid.NewGuid()}"));
        var toolDetector = new ToolInstallDetector(loader, toolPaths, settings);
        var presets = new PresetCacheService(loader, new ExternalDataGateway(
            new PrivacyGuardHttpHandler(new PrivacyGuard(PrivacyAllowlist.DefaultHosts)) { InnerHandler = new StubMessageHandler() },
            new OperationProgressHub()));
        var readiness = new ReadinessScoreService(loader, toolDetector, settings, presets);
        var coexistence = new ExternalToolCoexistenceService(new FakeRunningProcessProbe([]));
        var artifactDetector = new InstallArtifactDetector(new FakeMsiProbe(false));
        var export = new SeedContributionExportService(
            readiness,
            detector,
            coexistence,
            prefs,
            settings,
            artifactDetector);

        var path = await export.ExportAsync();
        var json = await File.ReadAllTextAsync(path);
        Assert.Contains("redacted", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Environment.UserName, json);
    }

    [Fact]
    public async Task ReadinessScoreService_ReturnsBoundedScore()
    {
        var dataRoot = TestPaths.FindDataRoot();
        var loader = new JsonDataLoader(dataRoot);
        var settingsPath = Path.Combine(Path.GetTempPath(), $"3dgo-ready-{Guid.NewGuid()}.db");
        await using var settings = new SqliteSettingsStore(settingsPath);
        await settings.InitializeAsync();
        var toolPaths = new ToolPathResolver(Path.Combine(Path.GetTempPath(), $"3dgo-ready-tools-{Guid.NewGuid()}"));
        var toolDetector = new ToolInstallDetector(loader, toolPaths, settings);
        var presets = new PresetCacheService(loader, new ExternalDataGateway(
            new PrivacyGuardHttpHandler(new PrivacyGuard(PrivacyAllowlist.DefaultHosts)) { InnerHandler = new StubMessageHandler() },
            new OperationProgressHub()));
        var service = new ReadinessScoreService(loader, toolDetector, settings, presets);
        var catalog = await TestPaths.CreateDisplayAutoDetector().GetCatalogAsync();
        var display = catalog.FirstOrDefault();

        var score = await service.ComputeAsync(display, offlineOnboarding: true, muxWarning: null);

        Assert.InRange(score.Score, 0, 100);
        Assert.NotEmpty(score.Factors);
    }

    [Fact]
    public async Task ReadinessScoreService_ReflectsOfflineOnboarding()
    {
        var dataRoot = TestPaths.FindDataRoot();
        var loader = new JsonDataLoader(dataRoot);
        var settingsPath = Path.Combine(Path.GetTempPath(), $"3dgo-offline-{Guid.NewGuid()}.db");
        await using var settings = new SqliteSettingsStore(settingsPath);
        await settings.InitializeAsync();
        await settings.SetAsync("offline_onboarding", "true");
        var toolPaths = new ToolPathResolver(Path.Combine(Path.GetTempPath(), $"3dgo-offline-tools-{Guid.NewGuid()}"));
        var toolDetector = new ToolInstallDetector(loader, toolPaths, settings);
        var presets = new PresetCacheService(loader, new ExternalDataGateway(
            new PrivacyGuardHttpHandler(new PrivacyGuard(PrivacyAllowlist.DefaultHosts)) { InnerHandler = new StubMessageHandler() },
            new OperationProgressHub()));
        var service = new ReadinessScoreService(loader, toolDetector, settings, presets);
        var catalog = await TestPaths.CreateDisplayAutoDetector().GetCatalogAsync();

        var score = await service.ComputeAsync(catalog.FirstOrDefault(), offlineOnboarding: true, muxWarning: null);

        Assert.Contains(score.Factors, factor => factor.Contains("Offline onboarding", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class FakeRunningProcessProbe : IRunningProcessProbe
    {
        private readonly HashSet<string> _running;

        public FakeRunningProcessProbe(IEnumerable<string> running) =>
            _running = new HashSet<string>(running, StringComparer.OrdinalIgnoreCase);

        public bool IsProcessRunning(string processName) =>
            _running.Contains(processName);

        public IReadOnlyList<string> GetRunningFrom(params string[] processNames) =>
            processNames.Where(IsProcessRunning).ToList();
    }
}
