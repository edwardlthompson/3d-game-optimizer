using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Artwork;
using SpatialLabsOptimizer.Infrastructure.Compatibility;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Steam;
using SpatialLabsOptimizer.Infrastructure.Updates;

namespace SpatialLabsOptimizer.Infrastructure.Library;

public sealed class LibraryExternalGamesMerger
{
    private readonly LaunchReadinessService _readiness;
    private readonly GameDatabase _database;
    private readonly EpicGogLibraryScanner? _externalScanner;
    private readonly LocalGameFolderRepository? _localFolders;
    private readonly LocalFolderGameScanner? _localScanner;
    private readonly LocalGameInstallResolver? _localInstallResolver;
    private readonly UbisoftConnectScanner? _ubisoftScanner;

    public LibraryExternalGamesMerger(
        LaunchReadinessService readiness,
        GameDatabase database,
        EpicGogLibraryScanner? externalScanner = null,
        LocalGameFolderRepository? localFolders = null,
        LocalFolderGameScanner? localScanner = null,
        LocalGameInstallResolver? localInstallResolver = null,
        UbisoftConnectScanner? ubisoftScanner = null)
    {
        _readiness = readiness;
        _database = database;
        _externalScanner = externalScanner;
        _localFolders = localFolders;
        _localScanner = localScanner;
        _localInstallResolver = localInstallResolver;
        _ubisoftScanner = ubisoftScanner;
    }

    public async Task MergeAsync(
        HashSet<int> steamInstalled,
        IDisplayVendorAdapter? adapter,
        CancellationToken cancellationToken)
    {
        var activeInstallIds = new List<int>();

        if (_externalScanner is not null)
        {
            activeInstallIds.AddRange(await MergeStoreListAsync(
                _externalScanner.ScanEpicInstalledGames(), steamInstalled, adapter, cancellationToken));
            activeInstallIds.AddRange(await MergeStoreListAsync(
                _externalScanner.ScanGogInstalledGames(), steamInstalled, adapter, cancellationToken));
        }

        if (_localFolders is not null && _localScanner is not null)
        {
            activeInstallIds.AddRange(await MergeLocalGamesAsync(steamInstalled, adapter, cancellationToken));
        }

        if (_ubisoftScanner is not null)
        {
            await MergeUbisoftGamesAsync(_ubisoftScanner.ScanInstalledGames(), steamInstalled, adapter, cancellationToken);
        }

        await _database.MarkLocalInstallsStaleExceptAsync(activeInstallIds, cancellationToken);

        if (_localInstallResolver is not null)
        {
            await _localInstallResolver.WarmCacheAsync(cancellationToken);
        }
    }

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
