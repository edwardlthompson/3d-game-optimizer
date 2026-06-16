using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Artwork;

namespace SpatialLabsOptimizer.Tests;

public class SteamCoverArtPolicyTests
{
    [Theory]
    [InlineData("Epic", false)]
    [InlineData("GOG", false)]
    [InlineData("Ubisoft", false)]
    [InlineData("Local", false)]
    [InlineData("Action", true)]
    [InlineData(null, true)]
    public void IsEligible_SkipsExternalStoreTags(string? reviewDescriptor, bool expected)
    {
        var game = new GameCatalogItem(
            123,
            "Test",
            CompatibilityTier.Experimental,
            LaunchReadinessState.Ready,
            true,
            null,
            null,
            null,
            null,
            null,
            reviewDescriptor,
            false);

        Assert.Equal(expected, SteamCoverArtPolicy.IsEligible(game));
    }
}
