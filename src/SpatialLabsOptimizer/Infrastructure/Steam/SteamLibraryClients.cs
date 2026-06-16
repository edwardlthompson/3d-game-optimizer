using System.Text.Json;
using SpatialLabsOptimizer.Infrastructure.Data;

namespace SpatialLabsOptimizer.Infrastructure.Steam;

internal static class SteamApiUrls
{
    public static string PlayerCount(int appId, string apiKey)
        => Build("https://api.steampowered.com/ISteamUserStats/GetNumberOfCurrentPlayers/v1/", new Dictionary<string, string>
        {
            ["appid"] = appId.ToString(),
            ["key"] = apiKey,
        });

    public static string OwnedGames(string apiKey, string steamId)
        => Build("https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/", new Dictionary<string, string>
        {
            ["key"] = apiKey,
            ["steamid"] = steamId,
            ["include_appinfo"] = "0",
            ["include_played_free_games"] = "1",
        });

    private static string Build(string baseUrl, IReadOnlyDictionary<string, string> query)
    {
        var builder = new UriBuilder(baseUrl);
        var parts = query.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}");
        builder.Query = string.Join("&", parts);
        return builder.Uri.AbsoluteUri;
    }
}

public sealed class PlayerCountService
{
    private readonly ExternalDataGateway _gateway;

    public PlayerCountService(ExternalDataGateway gateway)
    {
        _gateway = gateway;
    }

    public async Task<int?> GetCurrentPlayersAsync(int appId, string? apiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return null;
        }

        var url = SteamApiUrls.PlayerCount(appId, apiKey);
        var json = await _gateway.GetStringAsync(url, $"player-count-{appId}", cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("response", out var response) &&
            response.TryGetProperty("player_count", out var count))
        {
            return count.GetInt32();
        }

        return null;
    }
}

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

public sealed class SteamWebApiClient
{
    private readonly ExternalDataGateway _gateway;

    public SteamWebApiClient(ExternalDataGateway gateway)
    {
        _gateway = gateway;
    }

    public async Task<IReadOnlyList<int>> GetOwnedAppIdsAsync(string apiKey, string steamId, CancellationToken cancellationToken = default)
    {
        var url = SteamApiUrls.OwnedGames(apiKey, steamId);
        var json = await _gateway.GetStringAsync(url, "owned-games", cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<int>();
        }

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("response", out var response) ||
            !response.TryGetProperty("games", out var games))
        {
            return Array.Empty<int>();
        }

        return games.EnumerateArray()
            .Select(g => g.GetProperty("appid").GetInt32())
            .ToList();
    }
}
