using SpatialLabsOptimizer.Infrastructure.Install;

namespace SpatialLabsOptimizer.Infrastructure.Launch;

public sealed class SafeLaunchService
{
    private readonly IProcessLauncher _launcher;

    public SafeLaunchService(IProcessLauncher launcher)
    {
        _launcher = launcher;
    }

    public Task<bool> LaunchAsync(int appId, CancellationToken cancellationToken = default)
        => _launcher.TryStartSteamGameAsync(appId, cancellationToken);
}
