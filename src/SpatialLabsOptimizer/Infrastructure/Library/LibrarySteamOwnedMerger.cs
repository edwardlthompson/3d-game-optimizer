using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Artwork;
using SpatialLabsOptimizer.Infrastructure.Compatibility;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Steam;

namespace SpatialLabsOptimizer.Infrastructure.Library;

public sealed class LibrarySteamOwnedMerger
{
    private readonly LaunchReadinessService _readiness;
    private readonly GameDatabase _database;
    private readonly PlatformConnectionRepository? _connections;
    private readonly SteamWebApiClient? _steamWebApi;
    private readonly SteamStoreApiClient? _storeClient;

    public LibrarySteamOwnedMerger(
        LaunchReadinessService readiness,
        GameDatabase database,
        PlatformConnectionRepository? connections = null,
        SteamWebApiClient? steamWebApi = null,
        SteamStoreApiClient? storeClient = null)
    {
        _readiness = readiness;
        _database = database;
        _connections = connections;
        _steamWebApi = steamWebApi;
        _storeClient = storeClient;
    }

    public async Task MergeOwnedGamesAsync(
        HashSet<int> steamInstalled,
        IDisplayVendorAdapter? adapter,
        CancellationToken cancellationToken)
    {
        if (_connections is null || _steamWebApi is null || !await _connections.HasSteamCredentialsAsync(cancellationToken))
        {
            return;
        }

        var steamId = await _connections.GetSteamIdAsync(cancellationToken);
        var apiKey = await _connections.GetSteamApiKeyAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(steamId) || string.IsNullOrWhiteSpace(apiKey))
        {
            return;
        }

        IReadOnlyList<int> owned;
        try
        {
            owned = await _steamWebApi.GetOwnedAppIdsAsync(apiKey, steamId, cancellationToken);
        }
        catch (Exception)
        {
            return;
        }

        foreach (var appId in owned)
        {
            var existing = await _database.GetGameAsync(appId, cancellationToken);
            if (existing is not null)
            {
                if (!existing.IsInstalled && steamInstalled.Contains(appId))
                {
                    await _database.UpsertGameAsync(existing with { IsInstalled = true }, cancellationToken);
                }

                continue;
            }

            var title = $"Steam App {appId}";
            if (_storeClient is not null)
            {
                var details = await _storeClient.GetAppDetailsAsync(appId, cancellationToken);
                if (!string.IsNullOrWhiteSpace(details?.Name))
                {
                    title = details.Name;
                }
            }

            var tier = CompatibilityTier.Experimental;
            var isInstalled = steamInstalled.Contains(appId);
            var readiness = await _readiness.EvaluateAsync(appId, isInstalled, tier, cancellationToken);
            await _database.UpsertGameAsync(new GameCatalogItem(
                appId,
                title,
                tier,
                readiness,
                isInstalled,
                null,
                null,
                null,
                null,
                null,
                null,
                false), cancellationToken);
        }
    }
}

public sealed class LibraryStorePlaceholderAssigner
{
    private readonly GameDatabase _database;
    private readonly OperationProgressHub _progressHub;

    public LibraryStorePlaceholderAssigner(GameDatabase database, OperationProgressHub progressHub)
    {
        _database = database;
        _progressHub = progressHub;
    }

    public async Task AssignAsync(CancellationToken cancellationToken)
    {
        var allGames = await _database.GetAllGamesAsync(cancellationToken);
        foreach (var game in allGames)
        {
            if (game.SteamAppId > 0 || !string.IsNullOrWhiteSpace(game.CoverCachePath))
            {
                continue;
            }

            var storeTag = game.ReviewDescriptor ?? "Local";
            var placeholder = StoreCoverPlaceholder.ResolveBundledPath(storeTag);
            if (placeholder is null)
            {
                continue;
            }

            await _database.UpsertGameAsync(game with { CoverCachePath = placeholder }, cancellationToken);
            _progressHub.Publish(new OperationProgressReport(
                $"cover-{game.SteamAppId}",
                Application.Progress.OperationCategory.Download,
                "Store placeholder assigned",
                storeTag,
                IsComplete: true));
        }
    }
}
