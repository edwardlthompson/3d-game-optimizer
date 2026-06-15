using System.Text.Json;
using SpatialLabsOptimizer.Infrastructure.Data;

namespace SpatialLabsOptimizer.Infrastructure.Steam;

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

        var url = $"https://api.steampowered.com/ISteamUserStats/GetNumberOfCurrentPlayers/v1/?appid={appId}&key={apiKey}";
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
    public IReadOnlyList<int> ScanInstalledAppIds(string? steamPath = null)
    {
        steamPath ??= FindSteamPath();
        if (steamPath is null)
        {
            return Array.Empty<int>();
        }

        var steamApps = Path.Combine(steamPath, "steamapps");
        if (!Directory.Exists(steamApps))
        {
            return Array.Empty<int>();
        }

        var appIds = new List<int>();
        foreach (var manifest in Directory.EnumerateFiles(steamApps, "appmanifest_*.acf"))
        {
            var content = File.ReadAllText(manifest);
            var match = System.Text.RegularExpressions.Regex.Match(content, "\"appid\"\\s+\"(\\d+)\"");
            if (match.Success && int.TryParse(match.Groups[1].Value, out var appId))
            {
                appIds.Add(appId);
            }
        }

        return appIds;
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
        var url = $"https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/?key={apiKey}&steamid={steamId}&include_appinfo=0";
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
