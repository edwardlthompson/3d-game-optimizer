using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Install;
using SpatialLabsOptimizer.Infrastructure.Library;
using SpatialLabsOptimizer.Infrastructure.Progress;

namespace SpatialLabsOptimizer.Tests;

[Collection(ElevatedHelperInstallCollection.Name)]
public sealed class Sprint50Tests
{
    [Fact]
    public void GameLibraryItemViewModel_UpdateCover_RaisesCoverImageKey()
    {
        var item = SampleTile();
        var changed = new List<string>();
        item.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        item.UpdateCover(@"C:\covers\570.jpg");

        Assert.Contains(nameof(GameLibraryItemViewModel.CoverPath), changed);
        Assert.Contains(nameof(GameLibraryItemViewModel.CoverImageKey), changed);
        Assert.NotNull(item.CoverImageKey);
        Assert.Contains(@"C:\covers\570.jpg", item.CoverImageKey, StringComparison.Ordinal);
        Assert.Contains('|', item.CoverImageKey!);
    }

    [Fact]
    public void GameLibraryItemViewModel_UpdateCover_SamePath_BumpsRevision()
    {
        var item = SampleTile();
        item.UpdateCover(@"C:\covers\570.jpg");
        var firstKey = item.CoverImageKey;

        item.UpdateCover(@"C:\covers\570.jpg");

        Assert.NotEqual(firstKey, item.CoverImageKey);
    }

    [Fact]
    public async Task ToolManifest_LoadsBundledPackages()
    {
        var loader = new JsonDataLoader(TestPaths.FindDataRoot());
        var manifest = await loader.LoadAsync<ToolManifestDocument>("tools/tool-manifest-v1.json");

        Assert.NotNull(manifest);
        var reshade = manifest!.Tools!.First(t => t.Id == "reshade");
        var uevr = manifest.Tools.First(t => t.Id == "uevr");
        Assert.Equal("tools/fixtures/reshade-minimal.zip", reshade.BundledPackage);
        Assert.Equal("tools/fixtures/uevr-minimal.zip", uevr.BundledPackage);
        Assert.False(string.IsNullOrWhiteSpace(reshade.Sha256));
        Assert.False(string.IsNullOrWhiteSpace(uevr.Sha256));
    }

    [Fact]
    public async Task ElevatedHelper_InstallsBundledReShadeFixture()
    {
        ElevatedHelperInstallFixture.CleanupTool("reshade");

        var dataRoot = TestPaths.FindDataRoot();
        var bundledPath = Path.GetFullPath(Path.Combine(dataRoot, "tools/fixtures/reshade-minimal.zip"));
        Assert.True(File.Exists(bundledPath));

        var loader = new JsonDataLoader(dataRoot);
        var orchestrator = new SilentInstallOrchestrator(
            loader,
            new OperationProgressHub(),
            new InstallErrorCatalog(),
            new TestHelperLocator(TestPaths.FindElevatedHelperBuildOutput()));

        var exitCode = await orchestrator.InstallToolAsync(
            new ToolManifestEntry(
                "reshade",
                "ReShade",
                "",
                "FB79E924E9548B07FB1717805ED8B09DB7B82042352BD146FFF29126FB3E44BB",
                "",
                [0],
                null,
                "tools/fixtures/reshade-minimal.zip"));

        Assert.Equal(0, exitCode);

        var installRoot = ElevatedHelperInstallFixture.ToolInstallRoot("reshade");
        Assert.True(File.Exists(Path.Combine(installRoot, "ReShade.ini")));
    }

    private static GameLibraryItemViewModel SampleTile() =>
        new(new GameCatalogItem(
            570,
            "Dota 2",
            CompatibilityTier.Playable,
            LaunchReadinessState.Ready,
            true,
            null,
            null,
            null,
            null,
            null,
            null,
            false));
}
