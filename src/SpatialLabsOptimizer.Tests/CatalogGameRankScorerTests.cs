using SpatialLabsOptimizer.Infrastructure.Compatibility;

namespace SpatialLabsOptimizer.Tests;

public sealed class CatalogGameRankScorerTests
{
    [Fact]
    public void WeightedReviewScore_ShrinksLowSamplePerfectReviews()
    {
        var highSample = CatalogSteamRanking.WeightedReviewScore(100, 5000, 1200);
        var lowSample = CatalogSteamRanking.WeightedReviewScore(100, 3, 0);

        Assert.NotNull(highSample);
        Assert.NotNull(lowSample);
        Assert.True(highSample > lowSample);
    }

    [Fact]
    public void GameRankScore_BlendsSteamAndRank3D()
    {
        var score = CatalogGameRankScorer.Score(new CatalogGameRankInput(92, 8000, 500, 88));

        Assert.NotNull(score);
        Assert.InRange(score.Value, 70, 100);
    }

    [Fact]
    public void GameRankScore_FallsBackToRank3D_WhenSteamMissing()
    {
        var score = CatalogGameRankScorer.Score(new CatalogGameRankInput(null, null, null, 72));

        Assert.NotNull(score);
        Assert.True(score > 0);
    }

    [Fact]
    public void GameRankScore_ReturnsNull_WhenNoSignals()
    {
        Assert.Null(CatalogGameRankScorer.Score(new CatalogGameRankInput(null, null, null, 0)));
    }
}
