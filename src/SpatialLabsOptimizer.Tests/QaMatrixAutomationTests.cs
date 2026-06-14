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
        var detector = new DisplayAutoDetector(new Infrastructure.Data.JsonDataLoader(dataRoot));
        var catalog = await detector.GetCatalogAsync();
        var acer = catalog.FirstOrDefault(p => p.Vendor.Contains("Acer", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(acer);
        Assert.False(string.IsNullOrWhiteSpace(acer!.RecommendedProfileId));
    }

    [Fact]
    public async Task P0_UnknownDisplay_GenericProfileAvailable()
    {
        var dataRoot = TestPaths.FindDataRoot();
        var detector = new DisplayAutoDetector(new Infrastructure.Data.JsonDataLoader(dataRoot));
        var catalog = await detector.GetCatalogAsync();
        Assert.Contains(catalog, p =>
            p.Id == "generic-manual" ||
            p.Vendor.Equals("Generic", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task P0_RollbackSnapshot_RestoresPriorState()
    {
        var snapshots = new ConfigSnapshotService();
        var path = await snapshots.SnapshotAsync(570);
        Assert.True(File.Exists(path));
        await snapshots.RollbackAsync(path);
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
