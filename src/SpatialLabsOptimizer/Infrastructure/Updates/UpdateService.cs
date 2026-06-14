using System.Text.Json;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Settings;

namespace SpatialLabsOptimizer.Infrastructure.Updates;

public sealed class UpdateService
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

    public async Task<bool> IsCheckDueAsync(CancellationToken cancellationToken = default)
    {
        var interval = await _prefs.GetUpdateCheckIntervalAsync(cancellationToken);
        if (interval == UpdateCheckInterval.Off)
        {
            return false;
        }

        if (interval == UpdateCheckInterval.Startup)
        {
            return true;
        }

        var lastCheck = await _prefs.GetLastUpdateCheckUtcAsync(cancellationToken);
        if (lastCheck is null)
        {
            return true;
        }

        var elapsed = DateTimeOffset.UtcNow - lastCheck.Value;
        return interval switch
        {
            UpdateCheckInterval.Daily => elapsed >= TimeSpan.FromHours(24),
            UpdateCheckInterval.Weekly => elapsed >= TimeSpan.FromDays(7),
            _ => false
        };
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

                var assetMatch = ResolveAsset(release, version, artifactType);
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

    private static (string? DownloadUrl, InstallArtifactType ArtifactType, string AssetName)? ResolveAsset(
        JsonElement release,
        string version,
        InstallArtifactType artifactType)
    {
        if (!release.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var preferredName = $"SpatialLabsOptimizer-{version}-win-x64{InstallArtifactDetector.GetExtension(artifactType)}";
        string? fallbackUrl = null;
        string? fallbackName = null;

        foreach (var asset in assets.EnumerateArray())
        {
            if (!asset.TryGetProperty("name", out var nameElement) ||
                !asset.TryGetProperty("browser_download_url", out var urlElement))
            {
                continue;
            }

            var name = nameElement.GetString();
            var url = urlElement.GetString();
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(url))
            {
                continue;
            }

            if (!InstallArtifactDetector.MatchesExtension(name, artifactType))
            {
                continue;
            }

            if (string.Equals(name, preferredName, StringComparison.OrdinalIgnoreCase))
            {
                return (url, artifactType, name);
            }

            fallbackUrl ??= url;
            fallbackName ??= name;
        }

        return fallbackUrl is null || fallbackName is null
            ? null
            : (fallbackUrl, artifactType, fallbackName);
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
