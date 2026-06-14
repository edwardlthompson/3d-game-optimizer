using System.Diagnostics;
using SpatialLabsOptimizer.Infrastructure.Install;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Settings;

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

        StartHelper(helper, args);
        Environment.Exit(0);
        return Task.CompletedTask;
    }

    private static void StartHelper(string helperPath, string arguments)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = helperPath,
            Arguments = arguments,
            UseShellExecute = true
        });
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

public sealed class MsixUpdateApplier : IUpdateApplier
{
    public InstallArtifactType ArtifactType => InstallArtifactType.Msix;

    public async Task ApplyAsync(string stagedPath, CancellationToken cancellationToken = default)
    {
        var packageUri = new Uri(stagedPath);
        var deployment = new Windows.Management.Deployment.PackageManager();
        await deployment.AddPackageAsync(
            packageUri,
            null,
            Windows.Management.Deployment.DeploymentOptions.ForceApplicationShutdown);

        Environment.Exit(0);
    }
}

public sealed class UpdateApplyService
{
    private readonly UpdateDownloadService _download;
    private readonly IEnumerable<IUpdateApplier> _appliers;
    private readonly OperationProgressHub _progressHub;
    private readonly UserPreferencesService _prefs;
    private int _applyInFlight;

    public UpdateApplyService(
        UpdateDownloadService download,
        IEnumerable<IUpdateApplier> appliers,
        OperationProgressHub progressHub,
        UserPreferencesService prefs)
    {
        _download = download;
        _appliers = appliers;
        _progressHub = progressHub;
        _prefs = prefs;
    }

    public async Task ApplyUpdateAsync(UpdateCheckResult update, CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _applyInFlight, 1, 0) != 0)
        {
            throw new InvalidOperationException("Update already in progress.");
        }

        try
        {
            _progressHub.Publish(new OperationProgressReport(
                "update-apply",
                Application.Progress.OperationCategory.Update,
                "Applying update",
                "Downloading…"));

            var stagedPath = await _download.ResolveStagedPathAsync(update, cancellationToken);
            await UpdateStagedArtifactVerifier.VerifyAsync(stagedPath, cancellationToken);

            if (update.DownloadArtifactType is null)
            {
                throw new InvalidOperationException("Unknown install artifact type.");
            }

            var applier = _appliers.FirstOrDefault(a => a.ArtifactType == update.DownloadArtifactType.Value)
                ?? throw new InvalidOperationException($"No applier for {update.DownloadArtifactType}.");

            await _prefs.SetUpdateRestartPendingAsync(true, cancellationToken);
            if (!string.IsNullOrWhiteSpace(update.LatestVersion))
            {
                await _prefs.SetUpdateAppliedVersionAsync(update.LatestVersion, cancellationToken);
            }

            _progressHub.Publish(new OperationProgressReport(
                "update-apply",
                Application.Progress.OperationCategory.Update,
                "Applying update",
                "Restarting…"));

            await applier.ApplyAsync(stagedPath, cancellationToken);
        }
        finally
        {
            Interlocked.Exchange(ref _applyInFlight, 0);
        }
    }

}
