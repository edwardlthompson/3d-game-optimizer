using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Displays;

namespace SpatialLabsOptimizer.Tests;

public sealed class DisplayAutoDetectorTests
{
    [Fact]
    public async Task DetectAsync_MatchesAcerSpatialLabs15_ByPnpSignature()
    {
        var probe = new FakeDisplayEdidProbe(() =>
        [
            new DisplayEdidSnapshot("display1", "1022:abcd", "Acer SpatialLabs 15 Laptop Panel")
        ]);
        var loader = new JsonDataLoader(TestPaths.FindDataRoot());
        var detector = new DisplayAutoDetector(loader, probe);
        var profile = await detector.DetectAsync();

        Assert.NotNull(profile);
        Assert.Equal("acer-spatiallabs-15", profile!.Id);
    }

    [Fact]
    public async Task DetectAsync_FallsBackToGenericManual_WhenNoMatch()
    {
        var probe = new FakeDisplayEdidProbe(() =>
        [
            new DisplayEdidSnapshot("display1", "ffff:9999", "Unknown Panel")
        ]);
        var loader = new JsonDataLoader(TestPaths.FindDataRoot());
        var detector = new DisplayAutoDetector(loader, probe);
        var profile = await detector.DetectAsync();

        Assert.NotNull(profile);
        Assert.Equal("generic-manual", profile!.Id);
    }

    [Fact]
    public async Task DetectAsync_MatchesAcerSpatialLabs15_ByNameHint()
    {
        var probe = new FakeDisplayEdidProbe(() =>
        [
            new DisplayEdidSnapshot("display1", "unknown:sig", "Acer SpatialLabs 15 Panel")
        ]);
        var loader = new JsonDataLoader(TestPaths.FindDataRoot());
        var detector = new DisplayAutoDetector(loader, probe);
        var profile = await detector.DetectAsync();

        Assert.NotNull(profile);
        Assert.Equal("acer-spatiallabs-15", profile!.Id);
    }
}
