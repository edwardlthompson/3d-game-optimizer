using System.Text.Json;
using System.Text.RegularExpressions;

namespace SpatialLabsOptimizer.Infrastructure.Updates;

public sealed class EpicGogLibraryScanner
{
    private static readonly Regex GogInfoFilePattern = new(@"goggame-(\d+)\.info", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly string _epicManifestsPath;
    private readonly string _gogGamesPath;

    public EpicGogLibraryScanner()
        : this(null, null)
    {
    }

    public EpicGogLibraryScanner(string? epicManifestsPath, string? gogGamesPath)
    {
        _epicManifestsPath = epicManifestsPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Epic", "EpicGamesLauncher", "Data", "Manifests");
        _gogGamesPath = gogGamesPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "GOG Galaxy", "Games");
    }

    public IReadOnlyList<ExternalStoreGame> ScanEpicInstalledGames()
    {
        if (!Directory.Exists(_epicManifestsPath))
        {
            return Array.Empty<ExternalStoreGame>();
        }

        var games = new List<ExternalStoreGame>();
        foreach (var file in Directory.EnumerateFiles(_epicManifestsPath, "*.item"))
        {
            if (TryParseEpicManifest(file, out var game))
            {
                games.Add(game!);
            }
        }

        return games;
    }

    public IReadOnlyList<ExternalStoreGame> ScanGogInstalledGames()
    {
        if (!Directory.Exists(_gogGamesPath))
        {
            return Array.Empty<ExternalStoreGame>();
        }

        var games = new List<ExternalStoreGame>();
        foreach (var infoFile in Directory.EnumerateFiles(_gogGamesPath, "goggame-*.info", SearchOption.AllDirectories))
        {
            if (TryParseGogInfoFile(infoFile, out var game))
            {
                games.Add(game!);
            }
        }

        return games;
    }

    public IReadOnlyList<int> ScanEpicInstalledIds()
        => ScanEpicInstalledGames().Select(g => g.StableAppId).ToList();

    public IReadOnlyList<int> ScanGogInstalledIds()
        => ScanGogInstalledGames().Select(g => g.StableAppId).ToList();

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

        launchExe ??= PickLaunchExecutable(installLocation);
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
            ?? PickLaunchExecutable(rootPath);
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

    internal static string? PickLaunchExecutable(string installDir)
    {
        if (!Directory.Exists(installDir))
        {
            return null;
        }

        var candidates = Directory.EnumerateFiles(installDir, "*.exe", SearchOption.TopDirectoryOnly)
            .Where(p => !IsExcludedExecutable(Path.GetFileName(p)))
            .Select(p => new FileInfo(p))
            .OrderByDescending(f => f.Length)
            .ToList();

        return candidates.FirstOrDefault()?.FullName;
    }

    private static bool IsExcludedExecutable(string fileName)
        => fileName.StartsWith("unins", StringComparison.OrdinalIgnoreCase)
           || fileName.StartsWith("setup", StringComparison.OrdinalIgnoreCase)
           || fileName.Contains("redist", StringComparison.OrdinalIgnoreCase);
}
