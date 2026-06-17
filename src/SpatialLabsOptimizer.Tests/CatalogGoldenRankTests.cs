using System.Text.Json;
using SpatialLabsOptimizer.Infrastructure.Compatibility;

namespace SpatialLabsOptimizer.Tests;

public sealed class CatalogGoldenRankTests
{
    private static readonly string FixturesPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data", "rank-golden", "fixtures.json"));

    [Fact]
    public void GoldenFixtures_WeightedReview_InExpectedRanges()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(FixturesPath));
        foreach (var item in doc.RootElement.GetProperty("weightedReview").EnumerateArray())
        {
            var score = CatalogSteamRanking.WeightedReviewScore(
                item.GetProperty("reviewPercent").GetInt32(),
                item.GetProperty("reviewCount").GetInt32(),
                item.GetProperty("currentPlayers").GetInt32());

            Assert.NotNull(score);
            Assert.InRange(score.Value, item.GetProperty("expectedMin").GetDouble(), item.GetProperty("expectedMax").GetDouble());
        }
    }

    [Fact]
    public void GoldenFixtures_GameRank_InExpectedRanges()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(FixturesPath));
        foreach (var item in doc.RootElement.GetProperty("gameRank").EnumerateArray())
        {
            int? reviewPercent = item.TryGetProperty("reviewPercent", out var rp) && rp.ValueKind != JsonValueKind.Null
                ? rp.GetInt32()
                : null;
            int? reviewCount = item.TryGetProperty("reviewCount", out var rc) && rc.ValueKind != JsonValueKind.Null
                ? rc.GetInt32()
                : null;
            int? currentPlayers = item.TryGetProperty("currentPlayers", out var cp) && cp.ValueKind != JsonValueKind.Null
                ? cp.GetInt32()
                : null;

            var score = CatalogGameRankScorer.Score(new CatalogGameRankInput(
                reviewPercent,
                reviewCount,
                currentPlayers,
                item.GetProperty("rank3DScore").GetInt32()));

            Assert.NotNull(score);
            Assert.InRange(score.Value, item.GetProperty("expectedMin").GetDouble(), item.GetProperty("expectedMax").GetDouble());
        }
    }

    [Fact]
    public void GoldenFixtures_Rank3D_MatchesExpectedScore()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(FixturesPath));
        foreach (var item in doc.RootElement.GetProperty("rank3d").EnumerateArray())
        {
            var best = item.GetProperty("bestExperience");
            var input = new CatalogRank3DInput(
                item.GetProperty("bestLevel").GetString(),
                new CatalogRank3DPath(
                    best.GetProperty("platformKey").GetString()!,
                    best.GetProperty("label").GetString()!,
                    best.GetProperty("level").GetString()!),
                []);

            Assert.Equal(item.GetProperty("expectedScore").GetInt32(), CatalogRank3DScorer.Score(input));
        }
    }
}
