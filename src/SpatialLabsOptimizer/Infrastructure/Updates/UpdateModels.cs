namespace SpatialLabsOptimizer.Infrastructure.Updates;

public enum UpdateCheckInterval
{
    Off,
    Startup,
    Daily,
    Weekly
}

public enum InstallArtifactType
{
    Zip,
    Msi
}

public sealed record UpdateCheckResult(
    string CurrentVersion,
    string? LatestVersion,
    bool IsUpdateAvailable,
    string? ReleasePageUrl,
    string? DownloadUrl,
    InstallArtifactType? DownloadArtifactType,
    string? MatchedAssetName,
    string? ErrorMessage = null);

public static class SemverComparer
{
    public static int Compare(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) && string.IsNullOrWhiteSpace(right))
        {
            return 0;
        }

        if (string.IsNullOrWhiteSpace(left))
        {
            return -1;
        }

        if (string.IsNullOrWhiteSpace(right))
        {
            return 1;
        }

        var leftCore = StripPreRelease(left);
        var rightCore = StripPreRelease(right);
        if (Version.TryParse(NormalizeVersion(leftCore), out var lv) &&
            Version.TryParse(NormalizeVersion(rightCore), out var rv))
        {
            return lv.CompareTo(rv);
        }

        return string.Compare(leftCore, rightCore, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsNewer(string latest, string current) => Compare(latest, current) > 0;

    public static string? ParseTagVersion(string tag)
    {
        const string prefix = "SpatialLabsOptimizer-v";
        if (!tag.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return tag[prefix.Length..];
    }

    private static string StripPreRelease(string version)
    {
        var dash = version.IndexOf('-');
        return dash >= 0 ? version[..dash] : version;
    }

    private static string NormalizeVersion(string version)
    {
        var parts = version.Split('.');
        return parts.Length switch
        {
            1 => $"{parts[0]}.0.0",
            2 => $"{parts[0]}.{parts[1]}.0",
            _ => version
        };
    }
}

public static class ProductVersionReader
{
    public static string ReadCurrentVersion()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "product-version.json");
            if (File.Exists(path))
            {
                using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
                if (doc.RootElement.TryGetProperty("version", out var version))
                {
                    var value = version.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }
            }
        }
        catch (Exception)
        {
            // Fall back to assembly version.
        }

        var assemblyVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        return assemblyVersion is null
            ? "0.0.0"
            : $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}";
    }
}
