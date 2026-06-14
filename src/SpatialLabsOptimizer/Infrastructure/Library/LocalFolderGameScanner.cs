using SpatialLabsOptimizer.Infrastructure.Updates;

namespace SpatialLabsOptimizer.Infrastructure.Library;

public sealed record ScannedLocalGame(
    int StableAppId,
    string FolderPath,
    string LaunchExe,
    string DisplayTitle);

public sealed class LocalFolderGameScanner
{
    public const int MaxScanDepth = 2;

    private static readonly string[] ExcludedNameFragments =
    [
        "unins", "setup", "redist", "EasyAntiCheat", "BattlEye", "install", "vcredist", "dxsetup"
    ];

    public IReadOnlyList<ScannedLocalGame> ScanFolders(IReadOnlyList<string> watchFolders)
    {
        var results = new List<ScannedLocalGame>();
        var seenIds = new HashSet<int>();

        foreach (var root in watchFolders)
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            foreach (var gameFolder in EnumerateGameFolders(root))
            {
                var exe = PickLaunchExecutable(gameFolder);
                if (exe is null)
                {
                    continue;
                }

                var externalKey = $"{LocalGameFolderRepository.NormalizePath(gameFolder)}|{Path.GetFileName(exe)}";
                var stableId = ExternalStoreIdMapper.StableAppId("Local", externalKey);
                if (!seenIds.Add(stableId))
                {
                    continue;
                }

                results.Add(new ScannedLocalGame(
                    stableId,
                    gameFolder,
                    exe,
                    DeriveTitle(gameFolder, root)));
            }
        }

        return results;
    }

    public static bool IsExcludedExecutable(string fileName)
    {
        foreach (var fragment in ExcludedNameFragments)
        {
            if (fileName.Contains(fragment, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> EnumerateGameFolders(string watchRoot)
    {
        var folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { watchRoot };

        foreach (var dir in Directory.EnumerateDirectories(watchRoot, "*", SearchOption.TopDirectoryOnly))
        {
            folders.Add(dir);
            if (MaxScanDepth >= 2)
            {
                foreach (var nested in Directory.EnumerateDirectories(dir, "*", SearchOption.TopDirectoryOnly))
                {
                    folders.Add(nested);
                }
            }
        }

        return folders.Where(HasCandidateExecutable);
    }

    private static bool HasCandidateExecutable(string folder)
        => Directory.EnumerateFiles(folder, "*.exe", SearchOption.TopDirectoryOnly)
            .Any(p => !IsExcludedExecutable(Path.GetFileName(p)));

    private static string? PickLaunchExecutable(string gameFolder)
    {
        var candidates = Directory.EnumerateFiles(gameFolder, "*.exe", SearchOption.TopDirectoryOnly)
            .Where(p => !IsExcludedExecutable(Path.GetFileName(p)))
            .Select(p => new FileInfo(p))
            .OrderByDescending(f => f.Length)
            .ToList();

        return candidates.FirstOrDefault()?.FullName;
    }

    private static string DeriveTitle(string gameFolder, string watchRoot)
    {
        var name = Path.GetFileName(gameFolder.TrimEnd(Path.DirectorySeparatorChar));
        if (string.Equals(gameFolder, watchRoot, StringComparison.OrdinalIgnoreCase))
        {
            return name.Length > 0 ? name : "Local game";
        }

        return string.IsNullOrWhiteSpace(name) ? "Local game" : name;
    }
}
