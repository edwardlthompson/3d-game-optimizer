using SpatialLabsOptimizer.Infrastructure.Updates;

namespace SpatialLabsOptimizer.Tests;

public class InstallArtifactDetectorTests
{
    [Fact]
    public void Detect_ReturnsMsi_WhenMsiInstalled()
    {
        var detector = new InstallArtifactDetector(new FakeMsiProbe(true));
        Assert.Equal(InstallArtifactType.Msi, detector.Detect());
    }

    [Fact]
    public void Detect_ReturnsZip_ByDefault()
    {
        var detector = new InstallArtifactDetector(new FakeMsiProbe(false));
        Assert.Equal(InstallArtifactType.Zip, detector.Detect());
    }

    [Fact]
    public void MatchesExtension_AcceptsZipAndMsi()
    {
        Assert.True(InstallArtifactDetector.MatchesExtension("SpatialLabsOptimizer-1.1.0-win-x64.zip", InstallArtifactType.Zip));
        Assert.True(InstallArtifactDetector.MatchesExtension("SpatialLabsOptimizer-1.1.0-win-x64.msi", InstallArtifactType.Msi));
        Assert.False(InstallArtifactDetector.MatchesExtension("SpatialLabsOptimizer-1.1.0-win-x64.zip", InstallArtifactType.Msi));
    }
}

internal sealed class FakeMsiProbe : IMsiInstallProbe
{
    public FakeMsiProbe(bool installed) => IsInstalledValue = installed;
    public bool IsInstalledValue { get; }
    public bool IsMsiInstalled() => IsInstalledValue;
}
