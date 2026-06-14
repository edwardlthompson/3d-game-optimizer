using System.Diagnostics;
using Microsoft.Data.Sqlite;
using SpatialLabsOptimizer.Application.Progress;
using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Install;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Launch.Coexistence;
using SpatialLabsOptimizer.Infrastructure.Library;
using SpatialLabsOptimizer.Infrastructure.Performance;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Steam;

namespace SpatialLabsOptimizer.Tests;

public class QaSmokeTests
{
    [Fact]
    public async Task SilentInstallOrchestrator_RecordsExpectedAuditSteps()
    {
        var dataRoot = TestPaths.FindDataRoot();
        var loader = new JsonDataLoader(dataRoot);
        var hub = new OperationProgressHub();
        var orchestrator = new SilentInstallOrchestrator(
            loader,
            hub,
            new InstallErrorCatalog(),
            new TestHelperLocator(TestPaths.FindElevatedHelperBuildOutput()));
        var reports = new List<OperationProgressReport>();
        orchestrator.ProgressChanged += (_, report) => reports.Add(report);
        hub.ProgressPublished += (_, report) => reports.Add(report);

        await orchestrator.ExecuteAsync();

        Assert.NotEmpty(reports);
        Assert.Contains(reports, r => r.IsComplete);
        Assert.All(reports.Where(r => !r.IsComplete), r =>
        {
            Assert.Equal("silent-install", r.OperationId);
            Assert.Equal(OperationCategory.Setup, r.Category);
            Assert.True(r.TotalSteps >= 1);
        });
    }

    [Fact]
    public async Task MuxGpuDetector_ReturnsStructuredWarning()
    {
        var detector = new MuxGpuDetector();
        var status = await detector.DetectAsync();
        Assert.NotNull(status);
        if (status.HasDualGpu)
        {
            Assert.False(string.IsNullOrWhiteSpace(status.WarningMessage));
            Assert.Contains("Hybrid", status.WarningMessage, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void LibrarySortService_Sorts700Games_Under200ms()
    {
        var service = new LibrarySortService();
        var games = Enumerable.Range(1, 700)
            .Select(i => new GameCatalogItem(
                i,
                $"Game {i}",
                CompatibilityTier.Playable,
                LaunchReadinessState.Ready,
                i % 3 == 0,
                100 + i,
                70 + (i % 30),
                100 + (i * 17),
                WilsonScoreCalculator.Compute(70 + (i % 30), 100 + (i * 17)),
                null,
                null,
                false))
            .ToList();

        var sw = Stopwatch.StartNew();
        var sorted = service.Sort(games, LibrarySortMode.SteamReviews);
        sw.Stop();

        Assert.Equal(700, sorted.Count);
        Assert.True(sw.ElapsedMilliseconds < 200, $"Sort took {sw.ElapsedMilliseconds}ms");
        Assert.True(sorted[0].ReviewSortScore >= sorted[^1].ReviewSortScore);
    }

    [Fact]
    public async Task LaunchPipeline_P0_RollbackSnapshotExists()
    {
        var snapshots = new ConfigSnapshotService();
        var path = await snapshots.SnapshotAsync(1091500);
        Assert.True(File.Exists(path));
        await snapshots.RollbackAsync(path);
    }

    [Fact]
    public async Task LaunchPipeline_P0_SteamUnavailableFallbackStillLaunches()
    {
        var registry = new LaunchAdapterRegistry(new LaunchAdapterBase[]
        {
            new SucceedingLaunchAdapter(LaunchPlatform.Uevr)
        });
        var fallback = new AutoFallbackLaunchService(registry);
        var plan = new ResolvedGameLaunchPlan(
            999999,
            "Offline Seed Title",
            LaunchPlatform.Uevr,
            CompatibilityTier.Playable,
            0.5,
            0.5,
            0.5,
            null,
            null,
            false);

        var result = await fallback.LaunchWithFallbackAsync(plan, LaunchContext.Standard);
        Assert.True(result.Success);
    }

    private sealed class SucceedingLaunchAdapter : LaunchAdapterBase
    {
        public SucceedingLaunchAdapter(LaunchPlatform platform) => Platform = platform;
        public override LaunchPlatform Platform { get; }
        public override Task<bool> LaunchAsync(
            ResolvedGameLaunchPlan plan,
            LaunchContext context,
            CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }

    [Fact]
    public void LaunchPipeline_P0_ErrorCatalogCoversRollbackScenario()
    {
        var catalog = new LaunchErrorCatalog();
        var (message, recovery) = catalog.Get("3DGO-0005");
        Assert.Contains("Rollback", recovery, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(message));
    }

    [Fact]
    public async Task SqliteSettingsStore_SurvivesSchemaMigration()
    {
        var path = Path.Combine(Path.GetTempPath(), $"3dgo-migrate-{Guid.NewGuid()}.db");

        await using (var conn = new SqliteConnection($"Data Source={path}"))
        {
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                CREATE TABLE settings (
                    key TEXT PRIMARY KEY,
                    value TEXT NOT NULL
                );
                INSERT INTO settings (key, value) VALUES ('disclaimer_accepted', 'true');
                INSERT INTO settings (key, value) VALUES ('simple_mode', 'false');
                """;
            await cmd.ExecuteNonQueryAsync();
        }

        var store = new SqliteSettingsStore(path);
        await store.InitializeAsync();
        Assert.True(await store.GetDisclaimerAcceptedAsync());
        await store.SetAsync("theme", "dark");
        Assert.Equal("dark", await store.GetAsync("theme"));
        await store.DisposeAsync();
    }

    private static string FindDataRoot() => TestPaths.FindDataRoot();
}
