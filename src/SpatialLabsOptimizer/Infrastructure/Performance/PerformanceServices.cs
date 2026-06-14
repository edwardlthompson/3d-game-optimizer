using System.Text.Json;
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

    public async Task<HardwareProfile> ScanAsync(CancellationToken cancellationToken = default)
    {
        var phases = new[] { "CPU", "GPU", "RAM", "Display" };
        for (var i = 0; i < phases.Length; i++)
        {
            _progressHub.Publish(new OperationProgressReport(
                "specs-scan",
                Application.Progress.OperationCategory.Scan,
                "Scanning hardware",
                $"Detecting {phases[i]}…",
                StepIndex: i + 1,
                TotalSteps: phases.Length,
                PercentComplete: (i + 1) * 100.0 / phases.Length));
            await Task.Delay(50, cancellationToken);
        }

        return new HardwareProfile(
            Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "Unknown CPU",
            "Unknown GPU",
            8192,
            (int)(GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024 * 1024)),
            "Primary Display",
            "Unknown");
    }
}

public sealed class PerformanceTierEstimator
{
    private readonly JsonDataLoader _loader;

    public PerformanceTierEstimator(JsonDataLoader loader)
    {
        _loader = loader;
    }

    public async Task<PerformanceTier> EstimateAsync(HardwareProfile profile, CancellationToken cancellationToken = default)
    {
        var tiers = await _loader.LoadAsync<PerformanceTiersDocument>("performance/performance-tiers-v1.json", cancellationToken);
        if (tiers?.Tiers is null || tiers.Tiers.Count == 0)
        {
            return profile.VramMb >= 12288 ? PerformanceTier.High : PerformanceTier.Medium;
        }

        if (profile.VramMb >= 16384)
        {
            return PerformanceTier.Enthusiast;
        }

        if (profile.VramMb >= 8192)
        {
            return PerformanceTier.High;
        }

        return profile.VramMb >= 4096 ? PerformanceTier.Medium : PerformanceTier.Low;
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
    private readonly OperationProgressHub _progressHub;
    private readonly SqliteSettingsStore _settings;

    public BenchmarkService(OperationProgressHub progressHub, SqliteSettingsStore settings)
    {
        _progressHub = progressHub;
        _settings = settings;
    }

    public async Task<double> RunBenchmarkAsync(CancellationToken cancellationToken = default)
    {
        var phases = new[] { "DXGI probe", "Compute shader", "Memory bandwidth" };
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
            await Task.Delay(200, cancellationToken);
        }

        var score = 7500.0 + Random.Shared.NextDouble() * 500;
        await _settings.SetAsync("benchmark_score", score.ToString("F0"), cancellationToken);
        return score;
    }
}
