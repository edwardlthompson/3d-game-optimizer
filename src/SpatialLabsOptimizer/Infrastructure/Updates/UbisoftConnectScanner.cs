using System.Text.Json;
using System.Text.RegularExpressions;

namespace SpatialLabsOptimizer.Infrastructure.Updates;

public sealed record UbisoftInstalledGame(string InstallId, string Title, int StableAppId);

public sealed class UbisoftConnectScanner
{
    private static readonly Regex InstallIdPattern = new(@"""install_id""\s*:\s*""([^""]+)""", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex GameNamePattern = new(@"""name""\s*:\s*""([^""]+)""", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string DefaultConfigPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Ubisoft Game Launcher", "cache", "configuration", "configurations");

    private readonly string _configPath;

    public UbisoftConnectScanner()
        : this(DefaultConfigPath)
    {
    }

    public UbisoftConnectScanner(string configPath)
    {
        _configPath = configPath;
    }

    public IReadOnlyList<UbisoftInstalledGame> ScanInstalledGames()
    {
        if (!Directory.Exists(_configPath))
        {
            return Array.Empty<UbisoftInstalledGame>();
        }

        var games = new List<UbisoftInstalledGame>();
        foreach (var file in Directory.EnumerateFiles(_configPath, "*.json", SearchOption.AllDirectories))
        {
            if (TryParseConfigFile(file, out var game))
            {
                games.Add(game!);
            }
        }

        return games;
    }

    public static bool TryParseConfigFile(string path, out UbisoftInstalledGame? game)
    {
        game = null;
        try
        {
            var content = File.ReadAllText(path);
            if (!content.Contains("install_id", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var installMatch = InstallIdPattern.Match(content);
            if (!installMatch.Success)
            {
                return false;
            }

            var installId = installMatch.Groups[1].Value;
            var nameMatch = GameNamePattern.Match(content);
            var title = nameMatch.Success ? nameMatch.Groups[1].Value : Path.GetFileNameWithoutExtension(path);
            game = new UbisoftInstalledGame(
                installId,
                title,
                ExternalStoreIdMapper.StableAppId("Ubisoft", installId));
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
}
