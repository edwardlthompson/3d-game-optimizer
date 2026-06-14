using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Launch.Coexistence;

namespace SpatialLabsOptimizer.Tests;

public class CoexistenceLaunchTests
{
    [Fact]
    public void ExternalToolCoexistenceService_BlocksLaunch_WhenTrainerRunningAndCoexistenceOff()
    {
        var probe = new FakeRunningProcessProbe(["WeMod"]);
        var service = new ExternalToolCoexistenceService(probe);

        var (shouldBlock, context) = service.Evaluate(trainerCoexistenceEnabled: false, modManagerCoexistenceEnabled: true);

        Assert.True(shouldBlock);
        Assert.Equal(CoexistenceLaunchPolicy.Block, context.Policy);
        Assert.Contains("WeMod", context.DetectedTools);
    }

    [Fact]
    public void ExternalToolCoexistenceService_BlocksLaunch_WhenModManagerRunningAndCoexistenceOff()
    {
        var probe = new FakeRunningProcessProbe(["Vortex"]);
        var service = new ExternalToolCoexistenceService(probe);

        var (shouldBlock, context) = service.Evaluate(trainerCoexistenceEnabled: true, modManagerCoexistenceEnabled: false);

        Assert.True(shouldBlock);
        Assert.Equal(CoexistenceLaunchPolicy.Block, context.Policy);
        Assert.Contains("Vortex", context.DetectedTools);
    }

    [Fact]
    public void ExternalToolCoexistenceService_Returns3DGO0004BlockPolicy_WhenTrainerConflict()
    {
        var probe = new FakeRunningProcessProbe(["Wand"]);
        var service = new ExternalToolCoexistenceService(probe);

        var (shouldBlock, _) = service.Evaluate(false, true);

        Assert.True(shouldBlock);
    }

    [Fact]
    public void ExternalToolCoexistenceService_UsesGameFirst_WhenCoexistenceOnAndToolRunning()
    {
        var probe = new FakeRunningProcessProbe(["WeMod", "ModOrganizer"]);
        var service = new ExternalToolCoexistenceService(probe);

        var (shouldBlock, context) = service.Evaluate(true, true);

        Assert.False(shouldBlock);
        Assert.Equal(CoexistenceLaunchPolicy.GameFirst, context.Policy);
        Assert.Equal(2, context.DetectedTools.Count);
    }

    [Fact]
    public async Task UevrLauncher_SkipsInjector_WhenGameFirstContext()
    {
        var install = new GameInstallInfo(@"C:\Games\Test", @"C:\Games\Test\game.exe");
        var resolver = new StubInstallPathResolver(null, install);
        var spy = new SpyProcessLauncher();
        var adapter = new UevrLauncher(
            resolver,
            spy,
            new ToolPathResolver(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())));

        var plan = SamplePlan(LaunchPlatform.Uevr);
        var context = new LaunchContext(CoexistenceLaunchPolicy.GameFirst, ["WeMod"]);

        var launched = await adapter.LaunchAsync(plan, context);

        Assert.True(launched);
        Assert.True(spy.DirectExeLaunchAttempted);
        Assert.False(spy.InjectorLaunchAttempted);
    }

    [Fact]
    public async Task UevrLauncher_UsesInjector_WhenStandardContext()
    {
        var install = new GameInstallInfo(@"C:\Games\Test", @"C:\Games\Test\game.exe");
        var toolsRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(Path.Combine(toolsRoot, "uevr"));
        var injectorPath = Path.Combine(toolsRoot, "uevr", "UEVRInjector.exe");
        await File.WriteAllTextAsync(injectorPath, string.Empty);

        var resolver = new StubInstallPathResolver(null, install);
        var spy = new SpyProcessLauncher();
        var adapter = new UevrLauncher(resolver, spy, new ToolPathResolver(toolsRoot));
        var plan = SamplePlan(LaunchPlatform.Uevr);

        await adapter.LaunchAsync(plan, LaunchContext.Standard);

        Assert.True(spy.InjectorLaunchAttempted);
    }

    [Fact]
    public async Task GameFirstLaunchOrchestrator_WaitsForProcess()
    {
        var install = new GameInstallInfo(@"C:\Games\Test", @"C:\Games\Test\game.exe");
        var resolver = new StubInstallPathResolver(null, install);
        var probe = new FakeRunningProcessProbe([]);
        var launcher = new SpyProcessLauncher();
        var orchestrator = new GameFirstLaunchOrchestrator(resolver, launcher, probe);

        var task = orchestrator.LaunchAsync(SamplePlan(LaunchPlatform.Uevr));
        await Task.Delay(100);
        probe.SetRunning("game");
        var result = await task;

        Assert.True(result);
        Assert.True(launcher.DirectExeLaunchAttempted);
    }

    private static ResolvedGameLaunchPlan SamplePlan(LaunchPlatform platform) =>
        new(570, "Dota 2", platform, CompatibilityTier.Playable, 0.5, 0.5, 0.5, null, null, false);

    private sealed class FakeRunningProcessProbe : IRunningProcessProbe
    {
        private HashSet<string> _running;

        public FakeRunningProcessProbe(IEnumerable<string> running)
        {
            _running = new HashSet<string>(running, StringComparer.OrdinalIgnoreCase);
        }

        public void SetRunning(string processName) => _running.Add(processName);

        public bool IsProcessRunning(string processName) =>
            _running.Contains(processName);

        public IReadOnlyList<string> GetRunningFrom(params string[] processNames) =>
            processNames.Where(IsProcessRunning).ToList();
    }

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
        public bool DirectExeLaunchAttempted { get; private set; }
        public bool InjectorLaunchAttempted { get; private set; }

        public Task<bool> TryStartSteamGameAsync(int steamAppId, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<bool> TryStartAsync(string fileName, string? arguments, CancellationToken cancellationToken = default)
        {
            if (fileName.Contains("UEVRInjector", StringComparison.OrdinalIgnoreCase))
            {
                InjectorLaunchAttempted = true;
            }
            else
            {
                DirectExeLaunchAttempted = true;
            }

            return Task.FromResult(true);
        }
    }
}
