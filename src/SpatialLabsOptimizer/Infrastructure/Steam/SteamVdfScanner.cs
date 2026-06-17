namespace SpatialLabsOptimizer.Infrastructure.Steam;

public sealed class SteamVdfScanner
{
    private static readonly System.Text.RegularExpressions.Regex AppIdRegex =
        new("\"appid\"\\s+\"(\\d+)\"", System.Text.RegularExpressions.RegexOptions.Compiled);

    private static readonly System.Text.RegularExpressions.Regex LibraryPathRegex =
        new("\"path\"\\s+\"([^\"]+)\"", System.Text.RegularExpressions.RegexOptions.Compiled);

    public IReadOnlyList<int> ScanInstalledAppIds(string? steamPath = null)
    {
        steamPath ??= FindSteamPath();
        if (steamPath is null)
        {
            return Array.Empty<int>();
        }

        var appIds = new HashSet<int>();
        foreach (var steamApps in EnumerateSteamAppsPaths(steamPath))
        {
            foreach (var manifest in Directory.EnumerateFiles(steamApps, "appmanifest_*.acf"))
            {
                var content = File.ReadAllText(manifest);
                var match = AppIdRegex.Match(content);
                if (match.Success && int.TryParse(match.Groups[1].Value, out var appId))
                {
                    appIds.Add(appId);
                }
            }
        }

        return appIds.ToList();
    }

    public static IEnumerable<string> EnumerateSteamAppsPaths(string steamPath)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var defaultApps = Path.Combine(steamPath, "steamapps");
        if (Directory.Exists(defaultApps) && seen.Add(defaultApps))
        {
            yield return defaultApps;
        }

        var libraryFolders = Path.Combine(steamPath, "config", "libraryfolders.vdf");
        if (!File.Exists(libraryFolders))
        {
            yield break;
        }

        var content = File.ReadAllText(libraryFolders);
        foreach (System.Text.RegularExpressions.Match match in LibraryPathRegex.Matches(content))
        {
            var libraryRoot = match.Groups[1].Value.Replace("\\\\", "\\", StringComparison.Ordinal);
            var apps = Path.Combine(libraryRoot, "steamapps");
            if (Directory.Exists(apps) && seen.Add(apps))
            {
                yield return apps;
            }
        }
    }

    private static string? FindSteamPath()
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var defaultPath = Path.Combine(programFiles, "Steam");
        return Directory.Exists(defaultPath) ? defaultPath : null;
    }
}
