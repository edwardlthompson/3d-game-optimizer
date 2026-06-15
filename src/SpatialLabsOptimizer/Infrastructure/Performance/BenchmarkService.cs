using System.Diagnostics;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Progress;

namespace SpatialLabsOptimizer.Infrastructure.Performance;

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
