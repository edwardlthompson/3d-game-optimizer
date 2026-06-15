using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Install;
using SpatialLabsOptimizer.Infrastructure.Launch;

namespace SpatialLabsOptimizer.Tests;

public class QaMatrixAutomationTests
{
    [Fact]
    public async Task P0_AcerDisplay_RecommendedProfileFromCatalog()
    {
        var dataRoot = TestPaths.FindDataRoot();
        var detector = TestPaths.CreateDisplayAutoDetector();
        var catalog = await detector.GetCatalogAsync();
        var acer = catalog.FirstOrDefault(p => p.Vendor.Contains("Acer", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(acer);
        Assert.False(string.IsNullOrWhiteSpace(acer!.RecommendedProfileId));
    }

    [Fact]
    public async Task P0_UnknownDisplay_GenericProfileAvailable()
    {
        var dataRoot = TestPaths.FindDataRoot();
        var detector = TestPaths.CreateDisplayAutoDetector();
        var catalog = await detector.GetCatalogAsync();
        Assert.Contains(catalog, p =>
            p.Id == "generic-manual" ||
            p.Vendor.Equals("Generic", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task P0_RollbackSnapshot_RestoresPriorState()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"3dgo-snap-{Guid.NewGuid()}.db");
        var store = new Infrastructure.Data.SqliteSettingsStore(dbPath);
        await store.InitializeAsync();
        var overrides = new GameOverrideRepository(store);
        await overrides.SaveAsync(new GameOverride(570, 0.7, 0.5, LaunchPlatform.Uevr, false, "Auto"));
        var snapshots = new ConfigSnapshotService(overrides);
        var snapshotPath = await snapshots.SnapshotAsync(570);
        Assert.True(File.Exists(snapshotPath));

        await overrides.SaveAsync(new GameOverride(570, 0.2, 0.2, LaunchPlatform.ReShade, true, "Headset"));
        await snapshots.RollbackAsync(snapshotPath);

        var restored = await overrides.GetAsync(570);
        Assert.NotNull(restored);
        Assert.Equal(0.7, restored!.Depth);
        Assert.Equal("Auto", restored.PreferredOutput);
        await store.DisposeAsync();
    }

    [Fact]
    public void P0_SilentInstallFailure_ClassifiedWithRecoverySteps()
    {
        var catalog = new InstallErrorCatalog();
        var (code, message, recovery) = catalog.Classify(1, helperMissing: false);
        Assert.StartsWith("3DGO-", code);
        Assert.False(string.IsNullOrWhiteSpace(message));
        Assert.False(string.IsNullOrWhiteSpace(recovery));
    }

    [Fact]
    public async Task P0_OfflineSeed_SteamUnavailableLibraryStillLoads()
    {
        var dataRoot = TestPaths.FindDataRoot();
        var repo = new Infrastructure.Compatibility.CompatibilityRepository(
            new Infrastructure.Data.JsonDataLoader(dataRoot));
        var games = await repo.GetAllAsync();
        Assert.True(games.Count >= 3);
    }
}
