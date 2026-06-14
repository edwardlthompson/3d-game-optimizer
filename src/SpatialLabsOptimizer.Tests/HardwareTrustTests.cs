using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Performance;
using SpatialLabsOptimizer.Infrastructure.Progress;

namespace SpatialLabsOptimizer.Tests;

public class HardwareTrustTests
{
    [Fact]
    public void BenchmarkService_ComputeDeterministicScore_IsStable()
    {
        var first = BenchmarkService.ComputeDeterministicScore();
        var second = BenchmarkService.ComputeDeterministicScore();
        Assert.Equal(first, second);
    }

    [Fact]
    public async Task BenchmarkService_StoredScore_IsDeterministicAcrossRuns()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bench-trust-{Guid.NewGuid()}.db");
        var store = new SqliteSettingsStore(path);
        await store.InitializeAsync();
        var benchmark = new BenchmarkService(new OperationProgressHub(), store);

        var first = await benchmark.RunBenchmarkAsync();
        var second = await benchmark.RunBenchmarkAsync();

        Assert.Equal(first, second);
        await store.DisposeAsync();
    }

    [Fact]
    public async Task SystemSpecsScanner_ReturnsNonEmptyProfileOnWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var scanner = new SystemSpecsScanner(new OperationProgressHub());
        var profile = await scanner.ScanAsync();

        Assert.False(string.IsNullOrWhiteSpace(profile.CpuName));
        Assert.NotEqual("Unknown CPU", profile.CpuName);
        Assert.False(string.IsNullOrWhiteSpace(profile.GpuName));
        Assert.NotEqual("Unknown GPU", profile.GpuName);
        Assert.True(profile.VramMb > 0);
        Assert.True(profile.RamMb > 0);
        Assert.False(string.IsNullOrWhiteSpace(profile.DisplayName));
    }

    [Fact]
    public async Task PerformanceTierEstimator_UsesDetectedVram()
    {
        var loader = new JsonDataLoader(TestPaths.FindDataRoot());
        var estimator = new PerformanceTierEstimator(loader);

        var low = await estimator.EstimateAsync(new Domain.HardwareProfile("CPU", "GPU", 2048, 8192, "Display", "1.0"));
        var high = await estimator.EstimateAsync(new Domain.HardwareProfile("CPU", "GPU", 12288, 32768, "Display", "1.0"));

        Assert.Equal(Domain.PerformanceTier.Low, low);
        Assert.Equal(Domain.PerformanceTier.High, high);
    }
}
