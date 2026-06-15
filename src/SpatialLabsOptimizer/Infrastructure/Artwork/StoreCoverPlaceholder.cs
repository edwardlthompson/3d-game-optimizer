namespace SpatialLabsOptimizer.Infrastructure.Artwork;

public static class StoreCoverPlaceholder
{
    public static string GetPlaceholderFileName(string? storeTag) =>
        storeTag?.ToUpperInvariant() switch
        {
            "EPIC" => "placeholder-epic.png",
            "GOG" => "placeholder-gog.png",
            "UBISOFT" => "placeholder-ubisoft.png",
            _ => "placeholder-cover.png"
        };

    public static string? ResolveBundledPath(string? storeTag)
    {
        var fileName = GetPlaceholderFileName(storeTag);

        var path = Path.Combine(AppContext.BaseDirectory, "Assets", fileName);
        if (File.Exists(path))
        {
            return path;
        }

        var fallback = Path.Combine(AppContext.BaseDirectory, "Assets", "placeholder-cover.png");
        return File.Exists(fallback) ? fallback : null;
    }
}
