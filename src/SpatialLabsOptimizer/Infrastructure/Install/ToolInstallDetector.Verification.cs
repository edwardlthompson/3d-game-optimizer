using Microsoft.Win32;
using SpatialLabsOptimizer.Infrastructure.Launch;

namespace SpatialLabsOptimizer.Infrastructure.Install;

public sealed partial class ToolInstallDetector
{
    private const int ProgramFilesSubdirScanLimit = 8;

    private bool IsFilePresent(string toolId, string? pathHint)
    {
        if (string.IsNullOrWhiteSpace(pathHint))
        {
            return false;
        }

        return _toolPaths.ResolveExecutable(toolId, pathHint) is not null
            || _toolPaths.ResolveExecutable(toolId, pathHint, $"bin/{pathHint}") is not null;
    }

    private static bool IsRegistryKeyPresent(string? pathHint)
    {
        if (string.IsNullOrWhiteSpace(pathHint))
        {
            return false;
        }

        try
        {
            var parts = pathHint.Split('\\', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                return false;
            }

            var hive = parts[0].ToUpperInvariant() switch
            {
                "HKLM" => RegistryHive.LocalMachine,
                "HKCU" => RegistryHive.CurrentUser,
                _ => RegistryHive.LocalMachine
            };
            using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            using var key = baseKey.OpenSubKey(parts[1]);
            return key is not null;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private bool IsProcessOrFilePresent(string toolId, string? pathHint)
    {
        if (string.IsNullOrWhiteSpace(pathHint))
        {
            return false;
        }

        if (IsFilePresent(toolId, pathHint))
        {
            return true;
        }

        var localTools = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "3d-game-optimizer",
            "tools",
            toolId);
        if (Directory.Exists(localTools) &&
            Directory.EnumerateFiles(localTools, pathHint, SearchOption.AllDirectories).Any())
        {
            return true;
        }

        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        if (!Directory.Exists(programFiles))
        {
            return false;
        }

        try
        {
            return Directory.EnumerateFiles(programFiles, pathHint, SearchOption.TopDirectoryOnly).Any()
                || Directory.EnumerateDirectories(programFiles)
                    .Take(ProgramFilesSubdirScanLimit)
                    .Any(dir => Directory.EnumerateFiles(dir, pathHint, SearchOption.TopDirectoryOnly).Any());
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }

    private static bool IsToolDirectoryPresent(string toolId)
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "3d-game-optimizer",
            "tools",
            toolId);
        return Directory.Exists(path) && Directory.EnumerateFileSystemEntries(path).Any();
    }
}
