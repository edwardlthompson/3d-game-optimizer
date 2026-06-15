using SpatialLabsOptimizer.Infrastructure.Install;
using SpatialLabsOptimizer.Infrastructure.Pcvr;
using SpatialLabsOptimizer.Infrastructure.Performance;
using SpatialLabsOptimizer.Infrastructure.Progress;

namespace SpatialLabsOptimizer.Tests;

public sealed class Sprint45UxTests
{
    [Fact]
    public async Task SystemSpecsScanner_PublishesCompleteReport()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var hub = new OperationProgressHub();
        var reports = new List<OperationProgressReport>();
        hub.ProgressPublished += (_, report) => reports.Add(report);

        var scanner = new SystemSpecsScanner(hub);
        await scanner.ScanAsync();

        Assert.Contains(reports, r => r.OperationId == "specs-scan" && r.IsComplete);
    }

    [Fact]
    public async Task BenchmarkService_PublishesCompleteReport()
    {
        var path = Path.Combine(Path.GetTempPath(), $"s45-bench-{Guid.NewGuid()}.db");
        await using var store = new Infrastructure.Data.SqliteSettingsStore(path);
        await store.InitializeAsync();

        var hub = new OperationProgressHub();
        var reports = new List<OperationProgressReport>();
        hub.ProgressPublished += (_, report) => reports.Add(report);

        var benchmark = new BenchmarkService(hub, store);
        var score = await benchmark.RunBenchmarkAsync();

        Assert.True(score > 0);
        Assert.Contains(reports, r => r.OperationId == "benchmark" && r.IsComplete);
    }

    [Fact]
    public void CommandPaletteService_ExposesDescriptionsForQuickActions()
    {
        var palette = new CommandPaletteService();
        var entries = palette.Search("");

        Assert.Contains(entries, e => e.Title == "Quick Actions" || e.Description.Length > 0);
        Assert.All(entries, e => Assert.False(string.IsNullOrWhiteSpace(e.Description)));
    }

    [Fact]
    public void CommandPaletteService_IncludesRescanLibraryAction()
    {
        var palette = new CommandPaletteService();
        var rescan = palette.Search("").FirstOrDefault(c => c.Id == "rescan-library");

        Assert.NotNull(rescan);
        Assert.Contains("games", rescan!.Description, StringComparison.OrdinalIgnoreCase);
    }
}
