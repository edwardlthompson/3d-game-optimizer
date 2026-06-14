using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Install;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Launch.Coexistence;
using SpatialLabsOptimizer.Infrastructure.Progress;

namespace SpatialLabsOptimizer.Tests;

public class LaunchIntegrationTests
{
    [Fact]
    public async Task TrueGameLauncher_ReturnsFalse_WhenSteamMissing()
    {
        var resolver = new StubInstallPathResolver(null, null);
        var launcher = new ProcessLauncher(resolver);
        var adapter = new TrueGameLauncher(resolver, launcher);
        var plan = SamplePlan(LaunchPlatform.TrueGame);

        Assert.False(await adapter.LaunchAsync(plan, LaunchContext.Standard));
    }

    [Fact]
    public async Task UevrLauncher_ReturnsFalse_WhenNoInstallPathAndNoSteam()
    {
        var resolver = new StubInstallPathResolver(null, null);
        var launcher = new ProcessLauncher(resolver);
        var adapter = new UevrLauncher(
            resolver,
            launcher,
            new ToolPathResolver(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())));

        Assert.False(await adapter.LaunchAsync(SamplePlan(LaunchPlatform.Uevr), LaunchContext.Standard));
    }

    [Fact]
    public async Task LaunchAdapter_InvokesProcessLauncher_NotNoOp()
    {
        var resolver = new StubInstallPathResolver(null, null);
        var spy = new SpyProcessLauncher();
        var adapter = new TrueGameLauncher(resolver, spy);

        await adapter.LaunchAsync(SamplePlan(LaunchPlatform.TrueGame), LaunchContext.Standard);

        Assert.True(spy.SteamLaunchAttempted);
    }

    [Fact]
    public async Task SilentInstallOrchestrator_InstallToolAsync_UsesHelperWhenPresent()
    {
        var loader = new JsonDataLoader(TestPaths.FindDataRoot());
        var hub = new OperationProgressHub();
        var orchestrator = new SilentInstallOrchestrator(
            loader,
            hub,
            new InstallErrorCatalog(),
            new TestHelperLocator(TestPaths.FindElevatedHelperBuildOutput()));

        var exitCode = await orchestrator.InstallToolAsync(
            new ToolManifestEntry("reshade", "ReShade", "", "", "SILENT", [0], null));

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void InstallErrorCatalog_ClassifiesHelperMissing()
    {
        var catalog = new InstallErrorCatalog();
        var (_, message, _) = catalog.Classify(-1, helperMissing: true);
        Assert.Contains("helper", message, StringComparison.OrdinalIgnoreCase);
    }

    private static ResolvedGameLaunchPlan SamplePlan(LaunchPlatform platform) =>
        new(570, "Dota 2", platform, CompatibilityTier.Playable, 0.5, 0.5, 0.5, null, null, false);

    private sealed class StubInstallPathResolver : IGameInstallPathResolver
    {
        private readonly string? _steamExe;
        private readonly GameInstallInfo? _install;

        public StubInstallPathResolver(string? steamExe, GameInstallInfo? install)
        {
            _steamExe = steamExe;
            _install = install;
        }

        public GameInstallInfo? Resolve(int steamAppId) => _install;

        public string? FindSteamExecutable() => _steamExe;
    }

    private sealed class SpyProcessLauncher : IProcessLauncher
    {
        public bool SteamLaunchAttempted { get; private set; }

        public Task<bool> TryStartSteamGameAsync(int steamAppId, CancellationToken cancellationToken = default)
        {
            SteamLaunchAttempted = true;
            return Task.FromResult(false);
        }

        public Task<bool> TryStartAsync(string fileName, string? arguments, CancellationToken cancellationToken = default)
            => Task.FromResult(false);
    }
}
