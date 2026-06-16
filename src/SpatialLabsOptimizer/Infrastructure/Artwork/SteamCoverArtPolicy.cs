using SpatialLabsOptimizer.Domain;

namespace SpatialLabsOptimizer.Infrastructure.Artwork;

public static class SteamCoverArtPolicy
{
    private static readonly string[] ExternalStoreTags = ["Epic", "GOG", "Ubisoft", "Local"];

    public static bool IsEligible(GameCatalogItem? game)
    {
        if (game is null)
        {
            return true;
        }

        var tag = game.ReviewDescriptor;
        if (string.IsNullOrWhiteSpace(tag))
        {
            return true;
        }

        return !ExternalStoreTags.Contains(tag, StringComparer.OrdinalIgnoreCase);
    }
}
