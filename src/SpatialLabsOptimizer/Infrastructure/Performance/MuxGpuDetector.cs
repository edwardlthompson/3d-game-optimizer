namespace SpatialLabsOptimizer.Infrastructure.Performance;

public sealed class MuxGpuDetector
{
    public Task<MuxGpuStatus> DetectAsync(CancellationToken cancellationToken = default)
    {
        // WMI probe placeholder — real implementation queries Win32_VideoController + MUX switch state.
        var hasMux = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER")?.Contains("Intel", StringComparison.OrdinalIgnoreCase) == true;
        return Task.FromResult(new MuxGpuStatus(
            hasMux,
            hasMux ? "Hybrid graphics detected — ensure the dGPU is active for 3D titles." : null));
    }
}

public sealed record MuxGpuStatus(bool HasDualGpu, string? WarningMessage);
