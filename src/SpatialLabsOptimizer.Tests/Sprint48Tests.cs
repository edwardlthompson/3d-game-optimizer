using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Install;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Media;
using SpatialLabsOptimizer.Infrastructure.Pcvr;
using SpatialLabsOptimizer.Infrastructure.Settings;

namespace SpatialLabsOptimizer.Tests;

public sealed class Sprint48Tests
{
    [Fact]
    public void LocalFileUriHelper_ToFileUri_UsesFileScheme()
    {
        var uri = LocalFileUriHelper.ToFileUri(@"C:\cache\covers\570.jpg");
        Assert.Equal("file", uri.Scheme);
        Assert.Contains("570.jpg", uri.AbsoluteUri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OpenXrRuntimeProbe_ReturnsNull_WhenOverrideIsOff()
    {
        Assert.Null(OpenXrRuntimeProbe.TryResolveActiveRuntimeLabel("off"));
    }

    [Fact]
    public void OpenXrRuntimePicker_IncludesOffOption()
    {
        var picker = new OpenXrRuntimePicker(new UserPreferencesService(
            new SqliteSettingsStore(Path.Combine(Path.GetTempPath(), $"3dgo-xr-{Guid.NewGuid()}.db"))));
        var options = picker.GetOptions();
        Assert.Contains(options, o => o.Id == "off");
    }

    [Fact]
    public async Task DisplayCatalog_LoadsRequiredToolIds()
    {
        var dataRoot = FindDataRoot();
        var detector = new DisplayAutoDetector(new JsonDataLoader(dataRoot), new NullDisplayEdidProbe());
        var catalog = await detector.GetCatalogAsync();
        var acer = catalog.FirstOrDefault(d => d.Id == "acer-psv27-2");
        Assert.NotNull(acer);
        Assert.Contains("uevr", acer.RequiredToolIds);
        Assert.Contains("reshade", acer.RequiredToolIds);
    }

    [Fact]
    public async Task ToolInstallDetector_ReturnsFalse_ForUnknownTool()
    {
        var dataRoot = FindDataRoot();
        var detector = new ToolInstallDetector(new JsonDataLoader(dataRoot), new ToolPathResolver());
        Assert.False(await detector.IsInstalledAsync("nonexistent-tool-id-xyz"));
    }

    private static string FindDataRoot()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "data");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("data root not found");
    }

    private sealed class NullDisplayEdidProbe : IDisplayEdidProbe
    {
        public IReadOnlyList<DisplayEdidSnapshot> GetCurrentSnapshots() => [];
    }
}
