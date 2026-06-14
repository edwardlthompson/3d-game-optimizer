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

            game = new ExternalStoreGame(
                "Epic",
                catalogId,
                ExternalStoreIdMapper.StableAppId("Epic", catalogId),
                string.IsNullOrWhiteSpace(title) ? "Epic game" : title);
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

            game = new ExternalStoreGame(
                "GOG",
                productId,
                ExternalStoreIdMapper.StableAppId("GOG", productId),
                title);
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
