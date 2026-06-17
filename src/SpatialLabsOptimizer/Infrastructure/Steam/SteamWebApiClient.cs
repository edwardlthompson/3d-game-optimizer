using System.Text.Json;
using SpatialLabsOptimizer.Infrastructure.Data;

namespace SpatialLabsOptimizer.Infrastructure.Steam;

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

        try
        {
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
        catch (JsonException)
        {
            return Array.Empty<int>();
        }
    }
}
