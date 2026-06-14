using SpatialLabsOptimizer.Domain;

namespace SpatialLabsOptimizer.Infrastructure.Launch.Coexistence;

public sealed class GameFirstLaunchOrchestrator
{
    private static readonly TimeSpan ProcessWaitTimeout = TimeSpan.FromSeconds(60);

    private readonly IGameInstallPathResolver _installPaths;
    private readonly IProcessLauncher _launcher;
    private readonly IRunningProcessProbe _probe;

    public GameFirstLaunchOrchestrator(
        IGameInstallPathResolver installPaths,
        IProcessLauncher launcher,
        IRunningProcessProbe probe)
    {
        _installPaths = installPaths;
        _launcher = launcher;
        _probe = probe;
    }

    public async Task<bool> LaunchAsync(ResolvedGameLaunchPlan plan, CancellationToken cancellationToken = default)
    {
        var install = _installPaths.Resolve(plan.SteamAppId);
        var started = install?.LaunchExecutable is not null
            ? await _launcher.TryStartAsync(install.LaunchExecutable, null, cancellationToken)
            : await _launcher.TryStartSteamGameAsync(plan.SteamAppId, cancellationToken);

        if (!started)
        {
            return false;
        }

        if (install?.LaunchExecutable is null)
        {
            return true;
        }

        var processName = Path.GetFileNameWithoutExtension(install.LaunchExecutable);
        return await WaitForProcessAsync(processName, cancellationToken);
    }

    private async Task<bool> WaitForProcessAsync(string processName, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow.Add(ProcessWaitTimeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_probe.IsProcessRunning(processName))
            {
                return true;
            }

            await Task.Delay(500, cancellationToken);
        }

        return false;
    }
}
