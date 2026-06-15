using System.Text.RegularExpressions;
using SpatialLabsOptimizer.Domain;

namespace SpatialLabsOptimizer.Infrastructure.Launch;

public sealed record GameInstallInfo(string InstallDir, string? LaunchExecutable);

public interface IGameInstallPathResolver
{
    GameInstallInfo? Resolve(int steamAppId);
    string? FindSteamExecutable();
}

public sealed class GameInstallPathResolver : IGameInstallPathResolver
{
    private static readonly Regex AppIdRegex = new("\"appid\"\\s+\"(\\d+)\"", RegexOptions.Compiled);
    private static readonly Regex InstallDirRegex = new("\"installdir\"\\s+\"([^\"]+)\"", RegexOptions.Compiled);
    private static readonly Regex LaunchExeRegex = new("\"launch\"\\s+\"([^\"]+)\"", RegexOptions.Compiled);

    public GameInstallInfo? Resolve(int steamAppId)
    {
        var steamPath = FindSteamExecutable();
        if (steamPath is null)
        {
            return null;
        }

        var steamRoot = Path.GetDirectoryName(steamPath);
        if (steamRoot is null)
        {
            return null;
        }

        var manifestPath = Path.Combine(steamRoot, "steamapps", $"appmanifest_{steamAppId}.acf");
        if (!File.Exists(manifestPath))
        {
            return null;
        }

        var content = File.ReadAllText(manifestPath);
        var appMatch = AppIdRegex.Match(content);
        if (!appMatch.Success || !int.TryParse(appMatch.Groups[1].Value, out var parsedId) || parsedId != steamAppId)
        {
            return null;
        }

        var dirMatch = InstallDirRegex.Match(content);
        if (!dirMatch.Success)
        {
            return null;
        }

        var installDir = Path.Combine(steamRoot, "steamapps", "common", dirMatch.Groups[1].Value);
        if (!Directory.Exists(installDir))
        {
            return null;
        }

        string? launchExe = null;
        var launchMatch = LaunchExeRegex.Match(content);
        if (launchMatch.Success)
        {
            var candidate = Path.Combine(installDir, launchMatch.Groups[1].Value);
            if (File.Exists(candidate))
            {
                launchExe = candidate;
            }
        }

        launchExe ??= Directory.EnumerateFiles(installDir, "*.exe", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(p => !Path.GetFileName(p).StartsWith("unins", StringComparison.OrdinalIgnoreCase));

        return new GameInstallInfo(installDir, launchExe);
    }

    public string? FindSteamExecutable()
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var candidate = Path.Combine(programFiles, "Steam", "steam.exe");
        return File.Exists(candidate) ? candidate : null;
    }
}
