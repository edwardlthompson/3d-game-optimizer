using System.Security.Cryptography;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Progress;

namespace SpatialLabsOptimizer.Infrastructure.Updates;

public sealed class UpdateDownloadService
{
    private readonly ExternalDataGateway _gateway;
    private readonly OperationProgressHub _progressHub;

    public UpdateDownloadService(ExternalDataGateway gateway, OperationProgressHub progressHub)
    {
        _gateway = gateway;
        _progressHub = progressHub;
    }

    public string? GetStagedArtifactPath(UpdateCheckResult update)
    {
        if (string.IsNullOrWhiteSpace(update.LatestVersion) ||
            string.IsNullOrWhiteSpace(update.MatchedAssetName))
        {
            return null;
        }

        var targetPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "3d-game-optimizer",
            "updates",
            update.LatestVersion,
            update.MatchedAssetName);

        return File.Exists(targetPath) ? targetPath : null;
    }

    public async Task<string> ResolveStagedPathAsync(
        UpdateCheckResult update,
        CancellationToken cancellationToken = default)
    {
        var existing = GetStagedArtifactPath(update);
        if (existing is not null)
        {
            await UpdateStagedArtifactVerifier.VerifyAsync(existing, cancellationToken);
            return existing;
        }

        return await DownloadAsync(update, cancellationToken);
    }

    public async Task<string> DownloadAsync(
        UpdateCheckResult update,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(update.DownloadUrl) ||
            string.IsNullOrWhiteSpace(update.LatestVersion) ||
            update.DownloadArtifactType is null ||
            string.IsNullOrWhiteSpace(update.MatchedAssetName))
        {
            throw new InvalidOperationException("Update metadata is incomplete.");
        }

        var stagingDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "3d-game-optimizer",
            "updates",
            update.LatestVersion);
        Directory.CreateDirectory(stagingDir);

        var targetPath = Path.Combine(stagingDir, update.MatchedAssetName);
        _progressHub.Publish(new OperationProgressReport(
            "update-download",
            Application.Progress.OperationCategory.Update,
            "Downloading update",
            update.MatchedAssetName,
            PercentComplete: 0));

        var progress = new Progress<(long transferred, long total)>(report =>
        {
            double? percent = report.total > 0 ? (double)report.transferred / report.total * 100 : null;
            _progressHub.Publish(new OperationProgressReport(
                "update-download",
                Application.Progress.OperationCategory.Update,
                "Downloading update",
                update.MatchedAssetName,
                PercentComplete: percent));
        });

        var bytes = await _gateway.GetBytesAsync(
            update.DownloadUrl,
            "update-download",
            progress,
            cancellationToken);

        if (bytes is null || bytes.Length == 0)
        {
            throw new InvalidOperationException("Download failed.");
        }

        await File.WriteAllBytesAsync(targetPath, bytes, cancellationToken);
        var hashPath = targetPath + ".sha256";
        await File.WriteAllTextAsync(hashPath, Convert.ToHexString(SHA256.HashData(bytes)), cancellationToken);

        _progressHub.Publish(new OperationProgressReport(
            "update-download",
            Application.Progress.OperationCategory.Update,
            "Downloading update",
            "Complete",
            PercentComplete: 100,
            IsComplete: true));

        return targetPath;
    }
}
