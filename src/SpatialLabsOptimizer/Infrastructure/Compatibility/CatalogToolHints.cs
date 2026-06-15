using SpatialLabsOptimizer.Domain;

namespace SpatialLabsOptimizer.Infrastructure.Compatibility;

public static class CatalogToolHints
{
    public static IReadOnlyList<string> GetRecommendedToolIds(CatalogGameMetadata? catalog)
    {
        if (catalog is null)
        {
            return Array.Empty<string>();
        }

        var tools = new List<string>();
        if (catalog.Platforms.Contains("uevr") || catalog.SourceIds.Contains("uevr-profiles"))
        {
            tools.Add("uevr");
        }

        if (catalog.SourceIds.Contains("nvidia-3d-vision"))
        {
            tools.Add("nvidia-3d-vision-manual");
        }

        if (catalog.Platforms.Contains("truegame"))
        {
            tools.Add("spatiallabs-runtime-platform");
        }

        if (catalog.Platforms.Contains("odyssey-hub"))
        {
            tools.Add("odyssey-hub");
        }

        if (catalog.BestLevel is "ultra3d" or "native3d" or "optimized3d")
        {
            tools.Add("reshade");
        }

        return tools.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static string DescribeRecommendedStack(CatalogGameMetadata? catalog)
    {
        var ids = GetRecommendedToolIds(catalog);
        if (ids.Count == 0)
        {
            return "";
        }

        return "Suggested tools: " + string.Join(", ", ids);
    }
}
