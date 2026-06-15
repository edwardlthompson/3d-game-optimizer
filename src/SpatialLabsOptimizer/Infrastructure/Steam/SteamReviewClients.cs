using System.Text.Json;
using SpatialLabsOptimizer.Infrastructure.Data;

namespace SpatialLabsOptimizer.Infrastructure.Steam;

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
