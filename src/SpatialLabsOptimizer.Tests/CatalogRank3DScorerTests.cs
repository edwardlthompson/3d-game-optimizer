using SpatialLabsOptimizer.Infrastructure.Compatibility;

namespace SpatialLabsOptimizer.Tests;

public sealed class CatalogRank3DScorerTests
{
    [Fact]
    public void Rank_UsesMethodScore_ForTrueGameUltra()
    {
        var input = new CatalogRank3DInput(
            "ultra3d",
            new CatalogRank3DPath("truegame", "Acer TrueGame · 3D Ultra", "ultra3d"),
            []);

        var rank = CatalogRank3DScorer.Rank(input);

        Assert.Equal(100, rank.Score);
    }

    [Fact]
    public void Rank_UsesBestPlatformSupport_WhenHigherThanBestExperience()
    {
        var input = new CatalogRank3DInput(
            "playable3d",
            new CatalogRank3DPath("uevr", "VRto3D · Playable", "playable3d"),
            [
                new CatalogRank3DPath("uevr", "Works Perfectly", "ultra3d"),
            ]);

        var rank = CatalogRank3DScorer.Rank(input);

        Assert.Equal(97, rank.Score);
        Assert.Equal("Works Perfectly", rank.Label);
    }

    [Fact]
    public void MatchesMinRank3D_FiltersByScore()
    {
        var catalog = new Domain.CatalogGameMetadata(
            "playable3d", ["uevr"], ["uevr-profiles"], null, false, 49, "UEVR · Playable");

        Assert.True(CatalogFilterHelper.MatchesMinRank3D(catalog, 42));
        Assert.False(CatalogFilterHelper.MatchesMinRank3D(catalog, 58));
    }
}
