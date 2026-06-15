using System.Text.Json;

namespace SpatialLabsOptimizer.Infrastructure.Updates;

public sealed partial class EpicGogLibraryScanner
{
    public static bool TryParseEpicManifest(string path, out ExternalStoreGame? game)
    {
        game = null;
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            var catalogId = root.TryGetProperty("CatalogItemId", out var catalogProp)
                ? catalogProp.GetString()
                : null;
            if (string.IsNullOrWhiteSpace(catalogId))
            {
                return false;
            }

            var title = root.TryGetProperty("DisplayName", out var displayProp)
                ? displayProp.GetString()
                : root.TryGetProperty("AppName", out var appProp)
                    ? appProp.GetString()
                    : Path.GetFileNameWithoutExtension(path);

            ResolveEpicInstallMetadata(root, out var installDir, out var launchExe);

            game = new ExternalStoreGame(
                "Epic",
                catalogId,
                ExternalStoreIdMapper.StableAppId("Epic", catalogId),
                string.IsNullOrWhiteSpace(title) ? "Epic game" : title,
                installDir,
                launchExe);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }

    internal static void ResolveEpicInstallMetadata(JsonElement root, out string? installDir, out string? launchExe)
    {
        installDir = null;
        launchExe = null;

        var installLocation = root.TryGetProperty("InstallLocation", out var installProp)
            ? installProp.GetString()
            : null;
        if (string.IsNullOrWhiteSpace(installLocation) || !Directory.Exists(installLocation))
        {
            return;
        }

        installDir = installLocation;
        var launchRelative = root.TryGetProperty("LaunchExecutable", out var launchProp)
            ? launchProp.GetString()
            : null;
        if (!string.IsNullOrWhiteSpace(launchRelative))
        {
            var candidate = Path.Combine(
                installLocation,
                launchRelative.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(candidate))
            {
                launchExe = candidate;
            }
        }

        launchExe ??= EpicGogLaunchExecutablePicker.PickLaunchExecutable(installLocation);
    }
}
