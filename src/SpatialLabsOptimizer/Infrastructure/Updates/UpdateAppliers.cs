using System.Diagnostics;
using SpatialLabsOptimizer.Infrastructure.Install;

namespace SpatialLabsOptimizer.Infrastructure.Updates;

public interface IUpdateApplier
{
    InstallArtifactType ArtifactType { get; }
    Task ApplyAsync(string stagedPath, CancellationToken cancellationToken = default);
}

public sealed class ZipUpdateApplier : IUpdateApplier
{
    private readonly IElevatedHelperLocator _helperLocator;

    public ZipUpdateApplier(IElevatedHelperLocator helperLocator)
    {
        _helperLocator = helperLocator;
    }

    public InstallArtifactType ArtifactType => InstallArtifactType.Zip;

    public Task ApplyAsync(string stagedPath, CancellationToken cancellationToken = default)
    {
        var installDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
        var pid = Environment.ProcessId;
        var helper = _helperLocator.HelperPath;
        var args =
            $"apply-update zip \"{stagedPath}\" --install-dir \"{installDir}\" --wait-pid {pid} --relaunch";

        Process.Start(new ProcessStartInfo
        {
            FileName = helper,
            Arguments = args,
            UseShellExecute = true
        });
        Environment.Exit(0);
        return Task.CompletedTask;
    }
}

public sealed class MsiUpdateApplier : IUpdateApplier
{
    private readonly IElevatedHelperLocator _helperLocator;

    public MsiUpdateApplier(IElevatedHelperLocator helperLocator)
    {
        _helperLocator = helperLocator;
    }

    public InstallArtifactType ArtifactType => InstallArtifactType.Msi;

    public Task ApplyAsync(string stagedPath, CancellationToken cancellationToken = default)
    {
        var pid = Environment.ProcessId;
        var helper = _helperLocator.HelperPath;
        var args = $"apply-update msi \"{stagedPath}\" --wait-pid {pid} --relaunch";
        Process.Start(new ProcessStartInfo
        {
            FileName = helper,
            Arguments = args,
            UseShellExecute = true
        });
        Environment.Exit(0);
        return Task.CompletedTask;
    }
}
