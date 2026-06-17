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

        var url = SteamApiUrls.PlayerCount(appId, apiKey);
        var json = await _gateway.GetStringAsync(url, $"player-count-{appId}", cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("response", out var response) &&
                response.TryGetProperty("player_count", out var count))
            {
                return count.GetInt32();
            }

            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
