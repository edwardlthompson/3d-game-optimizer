namespace SpatialLabsOptimizer.Infrastructure.Performance;

public sealed class MuxGpuDetector
{
    public Task<MuxGpuStatus> DetectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(DetectCore, cancellationToken);
    }

    private static MuxGpuStatus DetectCore()
    {
        var controllers = WmiHardwareProbe.QueryVideoControllers();
        var hasMux = WmiHardwareProbe.HasHybridGraphics(controllers);
        return new MuxGpuStatus(
            hasMux,
            hasMux ? "Hybrid graphics detected — ensure the dGPU is active for 3D titles." : null);
    }
}

public sealed record MuxGpuStatus(bool HasDualGpu, string? WarningMessage);
