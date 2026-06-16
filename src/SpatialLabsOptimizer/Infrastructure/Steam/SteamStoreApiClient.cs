using System.Text.Json;
using SpatialLabsOptimizer.Infrastructure.Data;

namespace SpatialLabsOptimizer.Infrastructure.Steam;

public sealed class SteamStoreApiClient
{
    private const int MaxCacheEntries = 500;
    private readonly ExternalDataGateway _gateway;
    private readonly Dictionary<int, SteamAppDetails> _cache = new();
    private readonly Queue<int> _cacheOrder = new();

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

        Remember(appId, details);
        return details;
    }

    private void Remember(int appId, SteamAppDetails details)
    {
        if (_cache.ContainsKey(appId))
        {
            return;
        }

        while (_cache.Count >= MaxCacheEntries && _cacheOrder.Count > 0)
        {
            _cache.Remove(_cacheOrder.Dequeue());
        }

        _cache[appId] = details;
        _cacheOrder.Enqueue(appId);
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
