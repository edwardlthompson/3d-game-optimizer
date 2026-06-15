using SpatialLabsOptimizer.Infrastructure.Artwork;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Media;
using SpatialLabsOptimizer.Infrastructure.Pcvr;
using SpatialLabsOptimizer.Infrastructure.Settings;

namespace SpatialLabsOptimizer.Tests;

public sealed class Sprint49Tests
{
    [Fact]
    public void LocalFileUriHelper_AppendsCacheBustQuery()
    {
        var temp = Path.Combine(Path.GetTempPath(), $"3dgo-uri-{Guid.NewGuid()}.png");
        File.WriteAllBytes(temp, [0x89, 0x50, 0x4E, 0x47]);
        try
        {
            var uri = LocalFileUriHelper.ToFileUri(temp, cacheBust: true);
            Assert.Contains("?v=", uri.AbsoluteUri, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(temp);
        }
    }

    [Fact]
    public void StoreCoverPlaceholder_MapsStoreTagsToDistinctFiles()
    {
        Assert.Equal("placeholder-epic.png", StoreCoverPlaceholder.GetPlaceholderFileName("Epic"));
        Assert.Equal("placeholder-gog.png", StoreCoverPlaceholder.GetPlaceholderFileName("GOG"));
        Assert.Equal("placeholder-ubisoft.png", StoreCoverPlaceholder.GetPlaceholderFileName("Ubisoft"));
        Assert.Equal("placeholder-cover.png", StoreCoverPlaceholder.GetPlaceholderFileName(null));
    }

    [Fact]
    public async Task PcvrRuntimeConnector_ReturnsNull_WhenOpenXrOff()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"3dgo-xr-off-{Guid.NewGuid()}.db");
        await using var settings = new SqliteSettingsStore(dbPath);
        await settings.InitializeAsync();
        var prefs = new UserPreferencesService(settings);
        await prefs.SetOpenXrRuntimeOverrideAsync("off");

        var connector = new PcvrRuntimeConnector(prefs);
        var runtime = await connector.DetectRuntimeAsync();

        Assert.Null(runtime);
    }

    [Fact]
    public async Task PcvrRuntimeConnector_LaunchViaOpenXr_ReturnsFalse_WhenOff()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"3dgo-xr-launch-{Guid.NewGuid()}.db");
        await using var settings = new SqliteSettingsStore(dbPath);
        await settings.InitializeAsync();
        var prefs = new UserPreferencesService(settings);
        await prefs.SetOpenXrRuntimeOverrideAsync("off");

        var connector = new PcvrRuntimeConnector(prefs);
        var launched = await connector.LaunchViaOpenXrAsync(570);

        Assert.False(launched);
    }
}
