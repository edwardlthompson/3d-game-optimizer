using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Artwork;
using SpatialLabsOptimizer.Infrastructure.Compatibility;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Updates;

namespace SpatialLabsOptimizer.Infrastructure.Library;

public sealed partial class LibraryExternalGamesMerger
{
    private async Task MergeUbisoftGamesAsync(
        IReadOnlyList<UbisoftInstalledGame> ubisoftGames,
        HashSet<int> steamInstalled,
        IDisplayVendorAdapter? adapter,
        CancellationToken cancellationToken)
    {
        foreach (var ubisoftGame in ubisoftGames)
        {
            if (steamInstalled.Contains(ubisoftGame.StableAppId))
            {
                continue;
            }

            var tier = CompatibilityTier.Experimental;
            var readiness = await _readiness.EvaluateAsync(ubisoftGame.StableAppId, true, tier, cancellationToken);
            var placeholder = StoreCoverPlaceholder.ResolveBundledPath("Ubisoft");
            await _database.UpsertGameAsync(new GameCatalogItem(
                ubisoftGame.StableAppId,
                $"Ubisoft: {ubisoftGame.Title}",
                tier,
                readiness,
                true,
                null,
                null,
                null,
                null,
                placeholder,
                "Ubisoft",
                false), cancellationToken);
        }
    }

    private async Task<IReadOnlyList<int>> MergeLocalGamesAsync(
        HashSet<int> steamInstalled,
        IDisplayVendorAdapter? adapter,
        CancellationToken cancellationToken)
    {
        var folders = await _localFolders!.GetFoldersAsync(cancellationToken);
        var scanned = _localScanner!.ScanFolders(folders);
        var activeIds = new List<int>();

        foreach (var localGame in scanned)
        {
            activeIds.Add(localGame.StableAppId);
            await _database.UpsertLocalInstallAsync(
                localGame.StableAppId,
                localGame.FolderPath,
                localGame.LaunchExe,
                localGame.DisplayTitle,
                isStale: false,
                cancellationToken);

            if (steamInstalled.Contains(localGame.StableAppId))
            {
                continue;
            }

            var tier = CompatibilityTier.Experimental;
            var readiness = await _readiness.EvaluateAsync(localGame.StableAppId, true, tier, cancellationToken);
            await _database.UpsertGameAsync(new GameCatalogItem(
                localGame.StableAppId,
                localGame.DisplayTitle,
                tier,
                readiness,
                true,
                null,
                null,
                null,
                null,
                null,
                "Local",
                false), cancellationToken);
        }

        return activeIds;
    }
}
