using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Artwork;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Library;
using SpatialLabsOptimizer.Infrastructure.Privacy;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Steam;

namespace SpatialLabsOptimizer.Tests;

public sealed class CoverArtPrefetchTests
{
    [Fact]
    public async Task PrefetchMissingArtworkAsync_SkipsCachedCovers()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"slo-cover-{Guid.NewGuid():N}.db");
        await using var database = new GameDatabase(dbPath);
        await database.InitializeAsync();

        var coverPath = Path.Combine(Path.GetTempPath(), $"cover-{Guid.NewGuid():N}.jpg");
        await File.WriteAllTextAsync(coverPath, "cached");

        await database.UpsertGameAsync(new GameCatalogItem(
            570,
            "Dota 2",
            CompatibilityTier.Playable,
            LaunchReadinessState.Ready,
            true,
            null,
            null,
            null,
            null,
            coverPath,
            null,
            false));

        var hub = new OperationProgressHub();
        var guard = new PrivacyGuard(PrivacyAllowlist.DefaultHosts);
        var gateway = new ExternalDataGateway(new PrivacyGuardHttpHandler(guard), hub);
        var artwork = new GameArtworkService(new SteamStoreApiClient(gateway), gateway, new CoverArtCache(), hub);
        var prefetch = new LibraryPrefetchService(database, artwork, hub);

        await prefetch.PrefetchMissingArtworkAsync([570]);

        Assert.True(File.Exists(coverPath));
    }
}
