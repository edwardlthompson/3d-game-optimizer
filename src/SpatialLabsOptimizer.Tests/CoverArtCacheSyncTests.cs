using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Artwork;
using SpatialLabsOptimizer.Infrastructure.Data;

namespace SpatialLabsOptimizer.Tests;

public sealed class CoverArtCacheSyncTests
{
    [Fact]
    public async Task SyncMissingPathsAsync_LinksDiskCacheToDatabase()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"3dgo-sync-{Guid.NewGuid()}.db");
        var cacheDir = Path.Combine(Path.GetTempPath(), $"3dgo-sync-cache-{Guid.NewGuid()}");
        Directory.CreateDirectory(cacheDir);
        var coverPath = Path.Combine(cacheDir, "1091500.jpg");
        await File.WriteAllBytesAsync(coverPath, [0xFF, 0xD8, 0xFF, 0xDB, 0x00, 0x10]);

        await using var database = new GameDatabase(dbPath);
        await database.InitializeAsync();
        await database.UpsertGameAsync(new GameCatalogItem(
            1091500,
            "Cyberpunk 2077",
            CompatibilityTier.Optimized,
            LaunchReadinessState.Ready,
            true,
            null,
            null,
            null,
            null,
            null,
            null,
            false,
            true));

        var cache = new CoverArtCache(cacheDir);
        await CoverArtCacheSync.SyncMissingPathsAsync(database, cache);

        var game = await database.GetGameAsync(1091500);
        Assert.Equal(coverPath, game?.CoverCachePath);
    }
}
