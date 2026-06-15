using System.Text.Json;
using System.Text.RegularExpressions;

namespace SpatialLabsOptimizer.Infrastructure.Updates;

public sealed partial class EpicGogLibraryScanner
{
    private static readonly Regex GogInfoFilePattern = new(@"goggame-(\d+)\.info", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static bool TryParseGogInfoFile(string path, out ExternalStoreGame? game)
    {
        game = null;
        try
        {
            var fileName = Path.GetFileName(path);
            var match = GogInfoFilePattern.Match(fileName);
            var productId = match.Success ? match.Groups[1].Value : null;
            var title = Path.GetFileName(Path.GetDirectoryName(path)) ?? "GOG game";

            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            if (root.TryGetProperty("gameId", out var gameIdProp))
            {
                productId = gameIdProp.GetRawText().Trim('"');
            }
            else if (root.TryGetProperty("gameID", out var legacyIdProp))
            {
                productId = legacyIdProp.GetRawText().Trim('"');
            }

            if (root.TryGetProperty("name", out var nameProp))
            {
                title = nameProp.GetString() ?? title;
            }

            if (string.IsNullOrWhiteSpace(productId))
            {
                return false;
            }

            ResolveGogInstallMetadata(root, path, out var installDir, out var launchExe);

            game = new ExternalStoreGame(
                "GOG",
                productId,
                ExternalStoreIdMapper.StableAppId("GOG", productId),
                title,
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

    internal static void ResolveGogInstallMetadata(JsonElement root, string infoFilePath, out string? installDir, out string? launchExe)
    {
        installDir = null;
        launchExe = null;

        var rootPath = root.TryGetProperty("rootPath", out var rootPathProp)
            ? rootPathProp.GetString()
            : root.TryGetProperty("path", out var pathProp)
                ? pathProp.GetString()
                : Path.GetDirectoryName(infoFilePath);

        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
        {
            return;
        }

        installDir = rootPath;
        launchExe = ResolveGogPlayTaskExecutable(root, rootPath)
            ?? ResolveGogExeProperty(root, rootPath)
            ?? EpicGogLaunchExecutablePicker.PickLaunchExecutable(rootPath);
    }

    private static string? ResolveGogPlayTaskExecutable(JsonElement root, string rootPath)
    {
        if (!root.TryGetProperty("playTasks", out var tasks) || tasks.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        string? fallback = null;
        foreach (var task in tasks.EnumerateArray())
        {
            var relPath = task.TryGetProperty("path", out var pathProp) ? pathProp.GetString() : null;
            if (string.IsNullOrWhiteSpace(relPath))
            {
                continue;
            }

            var candidate = Path.Combine(rootPath, relPath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(candidate))
            {
                continue;
            }

            var isPrimary = task.TryGetProperty("isPrimary", out var primaryProp) && primaryProp.GetBoolean();
            var taskType = task.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : null;
            if (isPrimary || string.Equals(taskType, "launch", StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }

            fallback ??= candidate;
        }

        return fallback;
    }

    private static string? ResolveGogExeProperty(JsonElement root, string rootPath)
    {
        var exe = root.TryGetProperty("exe", out var exeProp) ? exeProp.GetString() : null;
        if (string.IsNullOrWhiteSpace(exe))
        {
            return null;
        }

        var candidate = Path.Combine(rootPath, exe.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(candidate) ? candidate : null;
    }
}
