using SpatialLabsOptimizer.Infrastructure.Compatibility;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Updates;

namespace SpatialLabsOptimizer.Infrastructure.Library;

public sealed partial class LibraryExternalGamesMerger
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
}
