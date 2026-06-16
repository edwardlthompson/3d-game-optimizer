namespace SpatialLabsOptimizer.Infrastructure.Compatibility;

/// <summary>
/// Mirrors site/catalog/src/game-ranking.ts — 72% weighted Steam + 28% best 3D path score.
/// </summary>
public static class CatalogGameRankScorer
{
    private const double SteamWeight = 0.72;
    private const double Rank3DWeight = 0.28;

    public static double? Score(CatalogGameRankInput input)
    {
        var steam = CatalogSteamRanking.WeightedReviewScore(
            input.ReviewPercent,
            input.ReviewCount,
            input.CurrentPlayers);
        var rank3D = input.Rank3DScore;

        if (steam is null && rank3D <= 0)
        {
            return null;
        }

        var steamPart = steam ?? rank3D * 0.55;
        var score = steamPart * SteamWeight + rank3D * Rank3DWeight;
        return Math.Round(score, 1, MidpointRounding.AwayFromZero);
    }

    public static double? ScoreFromCatalogItem(
        int? reviewPercent,
        int? reviewCount,
        int? currentPlayers,
        int rank3DScore) =>
        Score(new CatalogGameRankInput(reviewPercent, reviewCount, currentPlayers, rank3DScore));
}

public sealed record CatalogGameRankInput(
    int? ReviewPercent,
    int? ReviewCount,
    int? CurrentPlayers,
    int Rank3DScore);
