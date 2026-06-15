using System.Text.Json;
using SpatialLabsOptimizer.Infrastructure.Data;

namespace SpatialLabsOptimizer.Infrastructure.Steam;

public sealed class SteamStoreApiClient
{
    private readonly ExternalDataGateway _gateway;
    private readonly Dictionary<int, SteamAppDetails> _cache = new();

    public SteamStoreApiClient(ExternalDataGateway gateway)
    {
        _gateway = gateway;
    }

    public async Task<SteamAppDetails?> GetAppDetailsAsync(int appId, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(appId, out var cached))
        {
            return cached;
        }

        var url = $"https://store.steampowered.com/api/appdetails?appids={appId}&l=english";
        var (json, statusCode) = await _gateway.TryGetStringAsync(
            url,
            $"steam-store-{appId}",
            cancellationToken,
            userMessage: $"Fetching Steam details for AppID {appId} (name, cover art)…");
        if (string.IsNullOrWhiteSpace(json) || statusCode >= 400)
        {
            return null;
        }

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty(appId.ToString(), out var root) ||
            !root.TryGetProperty("success", out var success) ||
            !success.GetBoolean())
        {
            return null;
        }

        if (!root.TryGetProperty("data", out var data))
        {
            return null;
        }

        var details = new SteamAppDetails
        {
            AppId = appId,
            Name = data.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
            HeaderImage = data.TryGetProperty("header_image", out var header) ? header.GetString() : null,
            CapsuleImage = data.TryGetProperty("capsule_imagev5", out var capsule) ? capsule.GetString() : null
        };

        if (data.TryGetProperty("genres", out var genres))
        {
            foreach (var genre in genres.EnumerateArray())
            {
                if (genre.TryGetProperty("description", out var desc))
                {
                    details.Genres.Add(desc.GetString() ?? "");
                }
            }
        }

        _cache[appId] = details;
        return details;
    }
}

public sealed class SteamAppReviewsClient
{
    private readonly ExternalDataGateway _gateway;

    public SteamAppReviewsClient(ExternalDataGateway gateway)
    {
        _gateway = gateway;
    }

    public async Task<(int percent, int count, double sortScore, string? descriptor)> GetReviewSummaryAsync(
        int appId,
        CancellationToken cancellationToken = default)
    {
        var url = $"https://store.steampowered.com/appreviews/{appId}?json=1&filter=all&language=english&num_per_page=0";
        var (json, statusCode) = await _gateway.TryGetStringAsync(
            url,
            $"steam-reviews-{appId}",
            cancellationToken,
            userMessage: $"Fetching Steam reviews for AppID {appId}…");
        if (string.IsNullOrWhiteSpace(json) || statusCode >= 400)
        {
            return (0, 0, 0, null);
        }

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("query_summary", out var summary))
        {
            return (0, 0, 0, null);
        }

        var total = summary.TryGetProperty("total_reviews", out var totalProp) ? totalProp.GetInt32() : 0;
        var score = summary.TryGetProperty("review_score", out var scoreProp) ? scoreProp.GetInt32() : 0;
        var desc = summary.TryGetProperty("review_score_desc", out var descProp) ? descProp.GetString() : null;
        var percent = score > 0 ? score * 10 : 0;
        var sortScore = WilsonScoreCalculator.Compute(percent, total);
        return (percent, total, sortScore, desc);
    }
}

public sealed class SteamAppDetails
{
    public int AppId { get; init; }
    public string Name { get; init; } = "";
    public string? HeaderImage { get; set; }
    public string? CapsuleImage { get; set; }
    public List<string> Genres { get; } = [];
    public int? ReviewScorePercent { get; set; }
    public int? ReviewCount { get; set; }
    public string? ReviewDescriptor { get; set; }
}

public sealed class SteamReviewService
{
    private readonly SteamStoreApiClient _storeClient;

    public SteamReviewService(SteamStoreApiClient storeClient)
    {
        _storeClient = storeClient;
    }

    public async Task<(int percent, int count, double sortScore, string? descriptor)> GetReviewDataAsync(
        int appId,
        CancellationToken cancellationToken = default)
    {
        var details = await _storeClient.GetAppDetailsAsync(appId, cancellationToken);
        var percent = details?.ReviewScorePercent ?? 0;
        var count = details?.ReviewCount ?? 0;
        var sortScore = WilsonScoreCalculator.Compute(percent, count);
        return (percent, count, sortScore, details?.ReviewDescriptor);
    }
}

public static class WilsonScoreCalculator
{
    public static double Compute(int reviewScorePercent, int reviewCount)
    {
        if (reviewCount <= 0)
        {
            return 0;
        }

        var p = reviewScorePercent / 100.0;
        var n = reviewCount;
        const double z = 1.96;
        var z2 = z * z;
        var numerator = p + z2 / (2 * n) - z * Math.Sqrt(p * (1 - p) / n + z2 / (4 * n * n));
        var denominator = 1 + z2 / n;
        return numerator / denominator;
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
