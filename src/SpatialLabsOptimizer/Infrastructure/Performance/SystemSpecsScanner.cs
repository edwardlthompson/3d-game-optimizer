using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Progress;

namespace SpatialLabsOptimizer.Infrastructure.Performance;

public sealed class SystemSpecsScanner
{
    private readonly OperationProgressHub _progressHub;

    public SystemSpecsScanner(OperationProgressHub progressHub)
    {
        _progressHub = progressHub;
    }

    public Task<HardwareProfile> ScanAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() => ScanCore(cancellationToken), cancellationToken);
    }

    private HardwareProfile ScanCore(CancellationToken cancellationToken)
    {
        var phases = new (string Label, Func<string> Probe)[]
        {
            ("CPU", WmiHardwareProbe.QueryCpuName),
            ("GPU", () => WmiHardwareProbe.SelectPrimaryGpu(WmiHardwareProbe.QueryVideoControllers())?.Name ?? "Unknown GPU"),
            ("RAM", () => $"{WmiHardwareProbe.QueryTotalRamMb()} MB"),
            ("Display", () => WmiHardwareProbe.SelectPrimaryGpu(WmiHardwareProbe.QueryVideoControllers())?.Name ?? "Primary Display")
        };

        string cpuName = "Unknown CPU";
        string gpuName = "Unknown GPU";
        string displayName = "Primary Display";
        string driverVersion = "Unknown";
        int vramMb = 0;
        int ramMb = 0;

        for (var i = 0; i < phases.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (label, probe) = phases[i];

            _progressHub.Publish(new OperationProgressReport(
                "specs-scan",
                Application.Progress.OperationCategory.Scan,
                "Scanning hardware",
                $"Detecting {label}…",
                StepIndex: i + 1,
                TotalSteps: phases.Length,
                PercentComplete: (i + 1) * 100.0 / phases.Length));

            switch (label)
            {
                case "CPU":
                    cpuName = WmiHardwareProbe.QueryCpuName();
                    break;
                case "GPU":
                case "Display":
                    var controllers = WmiHardwareProbe.QueryVideoControllers();
                    var primary = WmiHardwareProbe.SelectPrimaryGpu(controllers);
                    gpuName = primary?.Name ?? probe();
                    displayName = primary?.Name ?? "Primary Display";
                    driverVersion = primary?.DriverVersion ?? "Unknown";
                    vramMb = primary is null ? 0 : (int)Math.Max(0, primary.AdapterRamBytes / (1024 * 1024));
                    break;
                case "RAM":
                    ramMb = (int)Math.Min(int.MaxValue, WmiHardwareProbe.QueryTotalRamMb());
                    _ = probe();
                    break;
            }
        }

        if (vramMb <= 0)
        {
            vramMb = 4096;
        }

        _progressHub.Publish(new OperationProgressReport(
            "specs-scan",
            Application.Progress.OperationCategory.Scan,
            "Scanning hardware",
            "Hardware scan complete",
            IsComplete: true,
            PercentComplete: 100));

        return new HardwareProfile(cpuName, gpuName, vramMb, ramMb, displayName, driverVersion);
    }
}
