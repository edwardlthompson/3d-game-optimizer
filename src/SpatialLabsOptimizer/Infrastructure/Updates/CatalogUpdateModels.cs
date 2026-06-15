namespace SpatialLabsOptimizer.Infrastructure.Updates;

public sealed record CatalogUpdateResult(
    bool Success,
    string Message,
    int GameCount = 0,
    string? SyncStatus = null);

public static class CatalogUpdateUrls
{
    public const string DefaultCatalogJson =
        "https://edwardlthompson.github.io/3d-game-optimizer/catalog/data/catalog-v2.json";

    public const string DefaultCatalogHash =
        "https://edwardlthompson.github.io/3d-game-optimizer/catalog/data/catalog-v2.sha256";
}
