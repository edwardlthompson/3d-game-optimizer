using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Compatibility;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Updates;

namespace SpatialLabsOptimizer.Infrastructure.Library;

public sealed partial class LibraryExternalGamesMerger
{
    private async Task<IReadOnlyList<int>> MergeStoreListAsync(
        IReadOnlyList<ExternalStoreGame> externalGames,
        HashSet<int> steamInstalled,
        IDisplayVendorAdapter? adapter,
        CancellationToken cancellationToken)
    {
        var activeIds = new List<int>();

        foreach (var externalGame in externalGames)
        {
            if (await TryPersistInstallMetadataAsync(externalGame, cancellationToken))
            {
                activeIds.Add(externalGame.StableAppId);
            }

            if (steamInstalled.Contains(externalGame.StableAppId))
            {
                continue;
            }

            var tier = CompatibilityTier.Experimental;
            var readiness = await _readiness.EvaluateAsync(externalGame.StableAppId, true, tier, cancellationToken);
            await _database.UpsertGameAsync(new GameCatalogItem(
                externalGame.StableAppId,
                $"{externalGame.Store}: {externalGame.Title}",
                tier,
                readiness,
                true,
                null,
                null,
                null,
                null,
                null,
                externalGame.Store,
                false), cancellationToken);
        }

        return activeIds;
    }

    private async Task<bool> TryPersistInstallMetadataAsync(
        ExternalStoreGame externalGame,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(externalGame.InstallDir)
            || string.IsNullOrWhiteSpace(externalGame.LaunchExe)
            || !File.Exists(externalGame.LaunchExe))
        {
            return false;
        }

        await _database.UpsertLocalInstallAsync(
            externalGame.StableAppId,
            externalGame.InstallDir,
            externalGame.LaunchExe,
            $"{externalGame.Store}: {externalGame.Title}",
            isStale: false,
            cancellationToken);
        return true;
    }
}
