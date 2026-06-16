using System.Net.Http.Headers;
using System.Text.Json;
using SpatialLabsOptimizer.Infrastructure.Artwork;
using SpatialLabsOptimizer.Infrastructure.Data;

namespace SpatialLabsOptimizer.Infrastructure.Updates;

public sealed class SteamGridDbClient
{
    private readonly ExternalDataGateway _gateway;
    private readonly CoverArtCache _cache;

    public SteamGridDbClient(ExternalDataGateway gateway, CoverArtCache cache)
    {
        _gateway = gateway;
        _cache = cache;
    }

    public async Task<string?> ResolveCoverAsync(int appId, CancellationToken cancellationToken = default)
    {
        var apiKey = Environment.GetEnvironmentVariable("STEAMGRIDDB_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return null;
        }

        if (_cache.TryGetCached(appId, out var cached))
        {
            return cached;
        }

        var url = $"https://www.steamgriddb.com/api/v2/grids/game/{appId}";
        var (json, statusCode) = await _gateway.TryGetStringAsync(
            url,
            $"steamgrid-{appId}",
            cancellationToken,
            bearerToken: apiKey);
        if (string.IsNullOrWhiteSpace(json) || statusCode >= 400)
        {
            return null;
        }

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("data", out var data) || data.GetArrayLength() == 0)
        {
            return null;
        }

        var first = data[0];
        if (first.TryGetProperty("url", out var urlProp))
        {
            return urlProp.GetString();
        }

        return null;
    }
}
