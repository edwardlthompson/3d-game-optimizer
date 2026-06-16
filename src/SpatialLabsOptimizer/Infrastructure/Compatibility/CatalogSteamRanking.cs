namespace SpatialLabsOptimizer.Infrastructure.Compatibility;

/// <summary>
/// Mirrors site/catalog/src/steam-ranking.ts — weighted Steam popularity (reviews + players).
/// </summary>
public static class CatalogSteamRanking
{
    private const double PriorPercent = 75;
    private const double PriorReviewWeight = 200;

    public static double? WeightedReviewScore(int? reviewPercent, int? reviewCount, int? currentPlayers)
    {
        if (reviewPercent is null)
        {
            return null;
        }

        var reviews = reviewCount ?? 0;
        var players = currentPlayers ?? 0;

        var quality = (reviews * reviewPercent.Value + PriorReviewWeight * PriorPercent)
            / (reviews + PriorReviewWeight);

        var reviewSignal = Math.Log10(1 + reviews) / 5;
        var playerSignal = Math.Log10(1 + players) / 4;
        var credibility = Math.Min(1, reviewSignal * 0.7 + playerSignal * 0.3);

        var score = quality * (0.4 + 0.6 * credibility);
        return Math.Round(score, 1, MidpointRounding.AwayFromZero);
    }
}
