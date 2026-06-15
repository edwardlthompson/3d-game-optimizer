using System.Text.Json;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Settings;

namespace SpatialLabsOptimizer.Infrastructure.Updates;

public sealed partial class UpdateService
{
    private const string ReleasesUrl =
        "https://api.github.com/repos/edwardlthompson/3d-game-optimizer/releases";

    private readonly ExternalDataGateway _gateway;
    private readonly OperationProgressHub _progressHub;
    private readonly UserPreferencesService _prefs;
    private readonly InstallArtifactDetector _artifactDetector;

    public UpdateService(
        ExternalDataGateway gateway,
        OperationProgressHub progressHub,
        UserPreferencesService prefs,
        InstallArtifactDetector artifactDetector)
    {
        _gateway = gateway;
        _progressHub = progressHub;
        _prefs = prefs;
        _artifactDetector = artifactDetector;
    }

    public async Task<UpdateCheckResult> CheckForUpdateAsync(
        bool forceNetwork = true,
        CancellationToken cancellationToken = default)
    {
        var current = ProductVersionReader.ReadCurrentVersion();
        var artifactType = await _prefs.GetInstallArtifactTypeAsync(_artifactDetector, cancellationToken);

        if (!forceNetwork)
        {
            var cached = await _prefs.GetCachedUpdateResultAsync(cancellationToken);
            if (cached is not null)
            {
                return cached with { CurrentVersion = current };
            }
        }

        _progressHub.Publish(new OperationProgressReport(
            "update-check",
            Application.Progress.OperationCategory.Update,
            "Checking for updates",
            "Fetching release manifest…"));

        try
        {
            var json = await _gateway.GetStringAsync(ReleasesUrl, "github-releases", cancellationToken);
            if (string.IsNullOrWhiteSpace(json))
            {
                return await CacheAndReturnAsync(new UpdateCheckResult(
                    current, null, false, null, null, null, null, "Could not reach GitHub releases."), cancellationToken);
            }

            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return await CacheAndReturnAsync(new UpdateCheckResult(
                    current, null, false, null, null, null, null, "Unexpected GitHub response."), cancellationToken);
            }

            foreach (var release in doc.RootElement.EnumerateArray())
            {
                if (!release.TryGetProperty("tag_name", out var tagElement))
                {
                    continue;
                }

                var tag = tagElement.GetString();
                var version = tag is null ? null : SemverComparer.ParseTagVersion(tag);
                if (version is null)
                {
                    continue;
                }

                var releasePage = release.TryGetProperty("html_url", out var urlElement)
                    ? urlElement.GetString()
                    : null;

                var assetMatch = UpdateAssetResolver.ResolveAsset(release, version, artifactType);
                var isUpdateAvailable = SemverComparer.IsNewer(version, current);
                var result = new UpdateCheckResult(
                    current,
                    version,
                    isUpdateAvailable,
                    releasePage,
                    assetMatch?.DownloadUrl,
                    assetMatch?.ArtifactType,
                    assetMatch?.AssetName,
                    assetMatch is null && isUpdateAvailable
                        ? $"No {InstallArtifactDetector.GetExtension(artifactType)} installer published for this version yet."
                        : null);

                return await CacheAndReturnAsync(result, cancellationToken);
            }

            return await CacheAndReturnAsync(new UpdateCheckResult(
                current, null, false, null, null, null, null, "No product releases found."), cancellationToken);
        }
        catch (Exception ex)
        {
            var cached = await _prefs.GetCachedUpdateResultAsync(cancellationToken);
            if (cached is not null)
            {
                return cached with { CurrentVersion = current, ErrorMessage = ex.Message };
            }

            return new UpdateCheckResult(current, null, false, null, null, null, null, ex.Message);
        }
    }

    private async Task<UpdateCheckResult> CacheAndReturnAsync(
        UpdateCheckResult result,
        CancellationToken cancellationToken)
    {
        await _prefs.SetCachedUpdateResultAsync(result, cancellationToken);
        await _prefs.SetLastUpdateCheckUtcAsync(DateTimeOffset.UtcNow, cancellationToken);
        return result;
    }
}
