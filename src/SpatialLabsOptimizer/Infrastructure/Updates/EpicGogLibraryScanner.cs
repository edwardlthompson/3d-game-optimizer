using System.Text.RegularExpressions;

namespace SpatialLabsOptimizer.Infrastructure.Updates;

public sealed partial class EpicGogLibraryScanner
{
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
}
