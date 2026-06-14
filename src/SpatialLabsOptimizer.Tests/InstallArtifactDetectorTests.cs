using SpatialLabsOptimizer.Infrastructure.Updates;

namespace SpatialLabsOptimizer.Tests;

public class InstallArtifactDetectorTests
{
    [Fact]
    public void Detect_ReturnsMsix_WhenPackaged()
    {
        var detector = new InstallArtifactDetector(new FakePackageProbe(true), new FakeMsiProbe(false));
        Assert.Equal(InstallArtifactType.Msix, detector.Detect());
    }

    [Fact]
    public void Detect_ReturnsMsi_WhenMsiInstalled()
    {
        var detector = new InstallArtifactDetector(new FakePackageProbe(false), new FakeMsiProbe(true));
        Assert.Equal(InstallArtifactType.Msi, detector.Detect());
    }

    [Fact]
    public void Detect_ReturnsZip_ByDefault()
    {
        var detector = new InstallArtifactDetector(new FakePackageProbe(false), new FakeMsiProbe(false));
        Assert.Equal(InstallArtifactType.Zip, detector.Detect());
    }

    [Fact]
    public void MatchesExtension_AcceptsMsixBundle()
    {
        Assert.True(InstallArtifactDetector.MatchesExtension("SpatialLabsOptimizer-1.1.0-win-x64.msixbundle", InstallArtifactType.Msix));
        Assert.True(InstallArtifactDetector.MatchesExtension("SpatialLabsOptimizer-1.1.0-win-x64.zip", InstallArtifactType.Zip));
        Assert.False(InstallArtifactDetector.MatchesExtension("SpatialLabsOptimizer-1.1.0-win-x64.zip", InstallArtifactType.Msi));
    }
}

internal sealed class FakePackageProbe : IPackageInstallProbe
{
    public FakePackageProbe(bool packaged) => IsPackagedValue = packaged;
    public bool IsPackagedValue { get; }
    public bool IsPackaged() => IsPackagedValue;
}

internal sealed class FakeMsiProbe : IMsiInstallProbe
{
    public FakeMsiProbe(bool installed) => IsInstalledValue = installed;
    public bool IsInstalledValue { get; }
    public bool IsMsiInstalled() => IsInstalledValue;
}
