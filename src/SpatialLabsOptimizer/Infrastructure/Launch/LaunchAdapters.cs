using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Install;
using SpatialLabsOptimizer.Infrastructure.Launch.Coexistence;

namespace SpatialLabsOptimizer.Infrastructure.Launch;

public abstract class LaunchAdapterBase
{
    public abstract LaunchPlatform Platform { get; }

    public abstract Task<bool> LaunchAsync(
        ResolvedGameLaunchPlan plan,
        LaunchContext context,
        CancellationToken cancellationToken = default);
}

public sealed class TrueGameLauncher : LaunchAdapterBase
{
    private readonly IGameInstallPathResolver _installPaths;
    private readonly IProcessLauncher _launcher;

    public TrueGameLauncher(IGameInstallPathResolver installPaths, IProcessLauncher launcher)
    {
        _installPaths = installPaths;
        _launcher = launcher;
    }

    public override LaunchPlatform Platform => LaunchPlatform.TrueGame;

    public override async Task<bool> LaunchAsync(
        ResolvedGameLaunchPlan plan,
        LaunchContext context,
        CancellationToken cancellationToken = default)
    {
        var install = _installPaths.Resolve(plan.SteamAppId);
        if (install?.LaunchExecutable is not null)
        {
            return await _launcher.TryStartAsync(install.LaunchExecutable, null, cancellationToken);
        }

        return await _launcher.TryStartSteamGameAsync(plan.SteamAppId, cancellationToken);
    }
}

public sealed class UevrLauncher : LaunchAdapterBase
{
    private readonly IGameInstallPathResolver _installPaths;
    private readonly IProcessLauncher _launcher;
    private readonly ToolPathResolver _toolPaths;

    public UevrLauncher(
        IGameInstallPathResolver installPaths,
        IProcessLauncher launcher,
        ToolPathResolver toolPaths)
    {
        _installPaths = installPaths;
        _launcher = launcher;
        _toolPaths = toolPaths;
    }

    public override LaunchPlatform Platform => LaunchPlatform.Uevr;

    public override async Task<bool> LaunchAsync(
        ResolvedGameLaunchPlan plan,
        LaunchContext context,
        CancellationToken cancellationToken = default)
    {
        var install = _installPaths.Resolve(plan.SteamAppId);
        if (context.IsGameFirst)
        {
            if (install?.LaunchExecutable is not null)
            {
                return await _launcher.TryStartAsync(install.LaunchExecutable, null, cancellationToken);
            }

            return await _launcher.TryStartSteamGameAsync(plan.SteamAppId, cancellationToken);
        }

        var injector = _toolPaths.ResolveExecutable("uevr", "UEVRInjector.exe", "bin/UEVRInjector.exe");
        if (injector is not null && install?.LaunchExecutable is not null)
        {
            var args = $"\"{install.LaunchExecutable}\"";
            if (await _launcher.TryStartAsync(injector, args, cancellationToken))
            {
                return true;
            }
        }

        return await _launcher.TryStartSteamGameAsync(plan.SteamAppId, cancellationToken);
    }
}

public sealed class ReShadeLauncher : LaunchAdapterBase
{
    private readonly IGameInstallPathResolver _installPaths;
    private readonly IProcessLauncher _launcher;
    private readonly ToolConfigWriter _configWriter;

    public ReShadeLauncher(
        IGameInstallPathResolver installPaths,
        IProcessLauncher launcher,
        ToolConfigWriter configWriter)
    {
        _installPaths = installPaths;
        _launcher = launcher;
        _configWriter = configWriter;
    }

    public override LaunchPlatform Platform => LaunchPlatform.ReShade;

    public override async Task<bool> LaunchAsync(
        ResolvedGameLaunchPlan plan,
        LaunchContext context,
        CancellationToken cancellationToken = default)
    {
        var install = _installPaths.Resolve(plan.SteamAppId);
        if (install is not null && !context.IsGameFirst)
        {
            await _configWriter.ApplyReShadeConfigAsync(install.InstallDir, plan.Depth, plan.Convergence, cancellationToken);
        }

        if (install?.LaunchExecutable is not null)
        {
            return await _launcher.TryStartAsync(install.LaunchExecutable, null, cancellationToken);
        }

        return await _launcher.TryStartSteamGameAsync(plan.SteamAppId, cancellationToken);
    }
}

public sealed class LaunchAdapterRegistry
{
    private readonly IReadOnlyList<LaunchAdapterBase> _adapters;

    public LaunchAdapterRegistry(IEnumerable<LaunchAdapterBase> adapters)
    {
        _adapters = adapters.ToList();
    }

    public LaunchAdapterBase? GetAdapter(LaunchPlatform platform) =>
        _adapters.FirstOrDefault(a => a.Platform == platform);
}
