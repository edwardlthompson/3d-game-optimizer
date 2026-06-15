using System.Text.Json;
using SpatialLabsOptimizer.Infrastructure.Settings;

namespace SpatialLabsOptimizer.Infrastructure.Updates;

internal static class UpdateAssetResolver
{
    internal static (string? DownloadUrl, InstallArtifactType ArtifactType, string AssetName)? ResolveAsset(
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
}
