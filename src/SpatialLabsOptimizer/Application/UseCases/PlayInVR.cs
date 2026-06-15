using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Compatibility;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Install;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Launch.Coexistence;
using SpatialLabsOptimizer.Infrastructure.Pcvr;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Library;
using SpatialLabsOptimizer.Infrastructure.Settings;

namespace SpatialLabsOptimizer.Application.UseCases;

public sealed class PlayInVR
{
    private readonly PcvrRuntimeConnector _pcvr;
    private readonly CompatibilityRepository _compatibility;
    private readonly OperationProgressHub _progressHub;

    public PlayInVR(
        PcvrRuntimeConnector pcvr,
        CompatibilityRepository compatibility,
        OperationProgressHub progressHub)
    {
        _pcvr = pcvr;
        _compatibility = compatibility;
        _progressHub = progressHub;
    }

    public async Task<bool> ExecuteAsync(int appId, CancellationToken cancellationToken = default)
    {
        var entry = await _compatibility.GetByAppIdAsync(appId, cancellationToken);
        var launchOptions = entry?.SteamVrLaunchOptions;

        _progressHub.Publish(new OperationProgressReport(
            $"play-vr-{appId}",
            Application.Progress.OperationCategory.Launch,
            "Play in VR",
            "Probing PCVR runtime…",
            StepIndex: 1,
            TotalSteps: 3));

        var runtime = await _pcvr.DetectRuntimeAsync(cancellationToken);
        if (runtime is null)
        {
            _progressHub.Publish(new OperationProgressReport(
                $"play-vr-{appId}",
                Application.Progress.OperationCategory.Launch,
                "Play in VR",
                "No PCVR runtime detected",
                StepIndex: 3,
                TotalSteps: 3,
                IsComplete: true,
                IsFailed: true,
                ErrorMessage: "No PCVR runtime"));
            return false;
        }

        _progressHub.Publish(new OperationProgressReport(
            $"play-vr-{appId}",
            Application.Progress.OperationCategory.Launch,
            "Play in VR",
            $"Delegating to {runtime}",
            StepIndex: 2,
            TotalSteps: 3));

        var launched = runtime.StartsWith("OpenXR", StringComparison.Ordinal)
            ? await _pcvr.LaunchViaOpenXrAsync(appId, launchOptions, cancellationToken)
            : await _pcvr.LaunchViaSteamVrAsync(appId, launchOptions, cancellationToken);
        _progressHub.Publish(new OperationProgressReport(
            $"play-vr-{appId}",
            Application.Progress.OperationCategory.Launch,
            "Play in VR",
            launched ? "VR launch initiated" : "VR launch failed",
            StepIndex: 3,
            TotalSteps: 3,
            IsComplete: launched,
            IsFailed: !launched));

        return launched;
    }
}
