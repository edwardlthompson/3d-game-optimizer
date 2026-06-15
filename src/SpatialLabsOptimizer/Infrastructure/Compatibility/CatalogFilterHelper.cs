using SpatialLabsOptimizer.Domain;

namespace SpatialLabsOptimizer.Infrastructure.Compatibility;

public static class CatalogFilterHelper
{
    public static bool MatchesUltraNative(CatalogGameMetadata? catalog)
        => catalog is not null
           && (catalog.BestLevel is "ultra3d" or "native3d");

    public static bool MatchesTrueGame(CatalogGameMetadata? catalog)
        => catalog?.Platforms.Contains("truegame") == true;

    public static bool MatchesUevr(CatalogGameMetadata? catalog, VrCapability vr)
        => catalog?.Platforms.Contains("uevr") == true || vr == VrCapability.UevrCompatible;

    public static bool Matches3DVision(CatalogGameMetadata? catalog)
        => catalog?.SourceIds.Contains("nvidia-3d-vision") == true;

    public static string BuildSourceBadges(CatalogGameMetadata? catalog)
    {
        if (catalog is null)
        {
            return "";
        }

        var badges = new List<string>();
        if (catalog.BestLevel is "ultra3d")
        {
            badges.Add("3D Ultra");
        }
        else if (catalog.BestLevel is "native3d")
        {
            badges.Add("3D");
        }

        if (!string.IsNullOrWhiteSpace(catalog.TrueGameLabel))
        {
            badges.Add($"TrueGame {catalog.TrueGameLabel}");
        }

        if (catalog.SourceIds.Contains("uevr-profiles") || catalog.Platforms.Contains("uevr"))
        {
            badges.Add("UEVR");
        }

        if (catalog.SourceIds.Contains("nvidia-3d-vision"))
        {
            badges.Add(catalog.IsNvidia3DVisionLegacy ? "3D Vision (legacy)" : "3D Vision");
        }

        return string.Join(" · ", badges.Distinct(StringComparer.OrdinalIgnoreCase));
    }
}
