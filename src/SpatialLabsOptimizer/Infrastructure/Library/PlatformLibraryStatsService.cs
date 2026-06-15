using SpatialLabsOptimizer.Infrastructure.Compatibility;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Steam;
using SpatialLabsOptimizer.Infrastructure.Updates;

namespace SpatialLabsOptimizer.Infrastructure.Library;

public sealed record PlatformLibraryStats(
    int SteamInstalledLocal,
    int SteamOwnedOnline,
    int SteamCompatibilitySeed,
    int EpicInstalledLocal,
    int GogInstalledLocal,
    int UbisoftInstalledLocal,
    int CustomLocalFolders,
    int TotalInLibrary);

public sealed class PlatformLibraryStatsService
{
    private readonly SteamVdfScanner _vdfScanner;
    private readonly EpicGogLibraryScanner _epicGogScanner;
    private readonly UbisoftConnectScanner _ubisoftScanner;
    private readonly LocalGameFolderRepository _localFolders;
    private readonly LocalFolderGameScanner _localScanner;
    private readonly GameDatabase _database;
    private readonly CompatibilityRepository _compatibility;
    private readonly PlatformConnectionRepository _connections;
    private readonly SteamWebApiClient _steamWebApi;

    public PlatformLibraryStatsService(
        SteamVdfScanner vdfScanner,
        EpicGogLibraryScanner epicGogScanner,
        UbisoftConnectScanner ubisoftScanner,
        LocalGameFolderRepository localFolders,
        LocalFolderGameScanner localScanner,
        GameDatabase database,
        CompatibilityRepository compatibility,
        PlatformConnectionRepository connections,
        SteamWebApiClient steamWebApi)
    {
        _vdfScanner = vdfScanner;
        _epicGogScanner = epicGogScanner;
        _ubisoftScanner = ubisoftScanner;
        _localFolders = localFolders;
        _localScanner = localScanner;
        _database = database;
        _compatibility = compatibility;
        _connections = connections;
        _steamWebApi = steamWebApi;
    }

    public async Task<PlatformLibraryStats> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        await _database.InitializeAsync(cancellationToken);
        var seed = await _compatibility.GetAllAsync(cancellationToken);
        var steamInstalled = _vdfScanner.ScanInstalledAppIds();
        var ownedOnline = 0;
        if (await _connections.HasSteamCredentialsAsync(cancellationToken))
        {
            var steamId = await _connections.GetSteamIdAsync(cancellationToken);
            var apiKey = await _connections.GetSteamApiKeyAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(steamId) && !string.IsNullOrWhiteSpace(apiKey))
            {
                try
                {
                    var owned = await _steamWebApi.GetOwnedAppIdsAsync(apiKey!, steamId!, cancellationToken);
                    ownedOnline = Math.Max(0, owned.Count - steamInstalled.Count);
                }
                catch (Exception)
                {
                    ownedOnline = 0;
                }
            }
        }

        var epicPath = await _connections.GetEpicManifestsPathAsync(cancellationToken);
        var epicScanner = string.IsNullOrWhiteSpace(epicPath)
            ? _epicGogScanner
            : new EpicGogLibraryScanner(epicPath, null);
        var gogPath = await _connections.GetGogGamesPathAsync(cancellationToken);
        var gogScanner = string.IsNullOrWhiteSpace(gogPath)
            ? _epicGogScanner
            : new EpicGogLibraryScanner(null, gogPath);
        var ubiPath = await _connections.GetUbisoftConfigPathAsync(cancellationToken);
        var ubiScanner = string.IsNullOrWhiteSpace(ubiPath)
            ? _ubisoftScanner
            : new UbisoftConnectScanner(ubiPath);

        var folders = await _localFolders.GetFoldersAsync(cancellationToken);
        var localFolderGames = _localScanner.ScanFolders(folders).Count;
        var total = await _database.CountGamesAsync(cancellationToken);

        return new PlatformLibraryStats(
            steamInstalled.Count,
            ownedOnline,
            seed.Count,
            epicScanner.ScanEpicInstalledGames().Count,
            gogScanner.ScanGogInstalledGames().Count,
            ubiScanner.ScanInstalledGames().Count,
            localFolderGames,
            total);
    }
}
