using System.Diagnostics;
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

public sealed class PerformanceTierEstimator
{
    private readonly JsonDataLoader _loader;

    private static readonly Dictionary<string, int> DefaultMinVramMb = new(StringComparer.OrdinalIgnoreCase)
    {
        ["enthusiast"] = 16384,
        ["high"] = 8192,
        ["medium"] = 4096,
        ["low"] = 0
    };

    public PerformanceTierEstimator(JsonDataLoader loader)
    {
        _loader = loader;
    }

    public async Task<PerformanceTier> EstimateAsync(HardwareProfile profile, CancellationToken cancellationToken = default)
    {
        var tiers = await _loader.LoadAsync<PerformanceTiersDocument>("performance/performance-tiers-v1.json", cancellationToken);
        var rules = tiers?.Tiers?
            .Select(t => (Id: t.Id, MinVramMb: t.MinVramMb > 0 ? t.MinVramMb : DefaultMinVramMb.GetValueOrDefault(t.Id, 0)))
            .OrderByDescending(t => t.MinVramMb)
            .ToList();

        if (rules is null || rules.Count == 0)
        {
            return profile.VramMb >= 12288 ? PerformanceTier.High : PerformanceTier.Medium;
        }

        foreach (var rule in rules)
        {
            if (profile.VramMb >= rule.MinVramMb)
            {
                return rule.Id.ToLowerInvariant() switch
                {
                    "enthusiast" => PerformanceTier.Enthusiast,
                    "high" => PerformanceTier.High,
                    "medium" => PerformanceTier.Medium,
                    _ => PerformanceTier.Low
                };
            }
        }

        return PerformanceTier.Low;
    }

    private sealed class PerformanceTiersDocument
    {
        public List<TierRule> Tiers { get; set; } = [];
    }

    private sealed class TierRule
    {
        public string Id { get; set; } = "";
        public int MinVramMb { get; set; }
    }
}

public sealed class BenchmarkService
{
    private const int BenchmarkIterations = 4_000_000;

    private readonly OperationProgressHub _progressHub;
    private readonly SqliteSettingsStore _settings;

    public BenchmarkService(OperationProgressHub progressHub, SqliteSettingsStore settings)
    {
        _progressHub = progressHub;
        _settings = settings;
    }

    public async Task<double> RunBenchmarkAsync(CancellationToken cancellationToken = default)
    {
        var phases = new[] { "Integer math", "Floating-point mix", "Memory bandwidth" };
        for (var i = 0; i < phases.Length; i++)
        {
            _progressHub.Publish(new OperationProgressReport(
                "benchmark",
                Application.Progress.OperationCategory.Benchmark,
                "Running benchmark",
                phases[i],
                StepIndex: i + 1,
                TotalSteps: phases.Length,
                PercentComplete: (i + 1) * 100.0 / phases.Length));
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
        }

        var (score, elapsedMs) = await Task.Run(() =>
        {
            var result = ComputeDeterministicScore(out var ms);
            return (result, ms);
        }, cancellationToken);
        _progressHub.Publish(new OperationProgressReport(
            "benchmark",
            Application.Progress.OperationCategory.Benchmark,
            "Running benchmark",
            $"Completed in {elapsedMs:F0} ms",
            IsComplete: true,
            PercentComplete: 100));
        await _settings.SetAsync("benchmark_score", score.ToString("F0"), cancellationToken);
        return score;
    }

    public static double ComputeDeterministicScore(out double elapsedMs)
    {
        var sw = Stopwatch.StartNew();
        double checksum = 0;
        for (var i = 1; i <= BenchmarkIterations; i++)
        {
            checksum += Math.Sqrt(i) * Math.Sin(i * 0.001);
        }

        sw.Stop();
        elapsedMs = sw.Elapsed.TotalMilliseconds;
        return Math.Round(checksum, 1);
    }

    public static double ComputeDeterministicScore()
    {
        return ComputeDeterministicScore(out _);
    }
}
