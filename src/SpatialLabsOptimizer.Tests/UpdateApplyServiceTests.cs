using System.Net;
using System.Security.Cryptography;
using System.Text;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Privacy;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Settings;
using SpatialLabsOptimizer.Infrastructure.Updates;

namespace SpatialLabsOptimizer.Tests;

public class UpdateApplyServiceTests
{
    [Fact]
    public async Task StagedArtifactVerifier_ThrowsOnHashMismatch()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"3dgo-hash-{Guid.NewGuid()}");
        Directory.CreateDirectory(dir);
        var artifact = Path.Combine(dir, "SpatialLabsOptimizer-9.9.9-win-x64.zip");
        await File.WriteAllBytesAsync(artifact, Encoding.UTF8.GetBytes("corrupt-payload"));
        await File.WriteAllTextAsync(artifact + ".sha256", "deadbeef");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => UpdateStagedArtifactVerifier.VerifyAsync(artifact));

        Assert.Contains("3DGO-0103", ex.Message, StringComparison.Ordinal);
        Directory.Delete(dir, true);
    }

    [Fact]
    public async Task StagedArtifactVerifier_AcceptsMatchingHash()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"3dgo-hash-{Guid.NewGuid()}");
        Directory.CreateDirectory(dir);
        var bytes = Encoding.UTF8.GetBytes("valid-payload");
        var artifact = Path.Combine(dir, "SpatialLabsOptimizer-9.9.9-win-x64.zip");
        await File.WriteAllBytesAsync(artifact, bytes);
        await File.WriteAllTextAsync(artifact + ".sha256", Convert.ToHexString(SHA256.HashData(bytes)));

        await UpdateStagedArtifactVerifier.VerifyAsync(artifact);

        Directory.Delete(dir, true);
    }

    [Fact]
    public async Task ResolveStagedPath_ReusesValidStagedArtifactWithoutDownload()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "3d-game-optimizer",
            "updates",
            "9.9.9");
        Directory.CreateDirectory(dir);

        var assetName = "SpatialLabsOptimizer-9.9.9-win-x64.zip";
        var artifact = Path.Combine(dir, assetName);
        var bytes = Encoding.UTF8.GetBytes("staged-update");
        await File.WriteAllBytesAsync(artifact, bytes);
        await File.WriteAllTextAsync(artifact + ".sha256", Convert.ToHexString(SHA256.HashData(bytes)));

        var downloadCalled = false;
        var handler = new PrivacyGuardHttpHandler(new PrivacyGuard(PrivacyAllowlist.DefaultHosts))
        {
            InnerHandler = new ThrowIfCalledHandler(() => downloadCalled = true)
        };
        var hub = new OperationProgressHub();
        var gateway = new ExternalDataGateway(handler, hub);
        var download = new UpdateDownloadService(gateway, hub);

        var update = new UpdateCheckResult(
            "1.0.0",
            "9.9.9",
            true,
            "https://example.com/release",
            "https://example.com/download",
            InstallArtifactType.Zip,
            assetName);

        var resolved = await download.ResolveStagedPathAsync(update);

        Assert.Equal(artifact, resolved);
        Assert.False(downloadCalled);

        Directory.Delete(
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "3d-game-optimizer",
                "updates",
                "9.9.9"),
            true);
    }

    [Fact]
    public void GetStagedArtifactPath_ReturnsNullWhenMissing()
    {
        var hub = new OperationProgressHub();
        var handler = new PrivacyGuardHttpHandler(new PrivacyGuard(PrivacyAllowlist.DefaultHosts))
        {
            InnerHandler = new ThrowIfCalledHandler()
        };
        var download = new UpdateDownloadService(new ExternalDataGateway(handler, hub), hub);
        var update = new UpdateCheckResult(
            "1.0.0",
            "0.0.0-not-staged",
            true,
            null,
            "https://example.com/download",
            InstallArtifactType.Zip,
            "missing.zip");

        Assert.Null(download.GetStagedArtifactPath(update));
    }

    private sealed class ThrowIfCalledHandler : HttpMessageHandler
    {
        private readonly Action? _onCall;

        public ThrowIfCalledHandler(Action? onCall = null) => _onCall = onCall;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            _onCall?.Invoke();
            throw new InvalidOperationException("Download should not have been invoked.");
        }
    }
}
