using SpatialLabsOptimizer.Infrastructure.Artwork;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Library;
using SpatialLabsOptimizer.Infrastructure.Progress;
namespace SpatialLabsOptimizer.ViewModels;

public sealed partial class GameLibraryViewModel
{
    private void OnProgressPublished(object? sender, OperationProgressReport report)
    {
        if (report.OperationId.StartsWith("cover-", StringComparison.Ordinal) && report.IsComplete)
        {
            if (TryParseCoverAppId(report.OperationId, out var appId))
            {
                RunOnUiThread(() => _ = RefreshCoverTileAsync(appId));
            }

            return;
        }

        if (report.OperationId == "preset-prefetch" && report.IsComplete)
        {
            RunOnUiThread(() => _ = LoadFromCacheAsync());
            return;
        }

        if ((report.OperationId == "artwork-prefetch" || report.OperationId == "metadata-prefetch") && report.IsComplete)
        {
            RunOnUiThread(() => _ = HydrateCoverTilesAsync());
        }
    }

    private async Task HydrateCoverTilesAsync()
    {
        if (Games.Count == 0)
        {
            return;
        }

        using var gate = new SemaphoreSlim(4);
        var tasks = Games.Select(async item =>
        {
            await gate.WaitAsync();
            try
            {
                await HydrateCoverTileAsync(item);
            }
            finally
            {
                gate.Release();
            }
        });
        await Task.WhenAll(tasks);

        LibraryUpdated?.Invoke(this, EventArgs.Empty);
    }

    private async Task HydrateCoverTileAsync(GameLibraryItemViewModel item)
    {
        if (item.SteamAppId <= 0)
        {
            return;
        }

        var path = item.CoverPath;
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            return;
        }

        if (_coverCache.TryGetCached(item.SteamAppId, out var cached))
        {
            item.UpdateCover(cached);
            await SyncCoverPathToDatabaseAsync(item.SteamAppId, cached);
            return;
        }

        var game = await _database.GetGameAsync(item.SteamAppId);
        if (!string.IsNullOrWhiteSpace(game?.CoverCachePath) && File.Exists(game.CoverCachePath))
        {
            item.UpdateCover(game.CoverCachePath);
            return;
        }

        if (!SteamCoverArtPolicy.IsEligible(game))
        {
            return;
        }

        try
        {
            var resolved = await _artwork.ResolveCoverPathAsync(item.SteamAppId);
            if (!string.IsNullOrWhiteSpace(resolved) && File.Exists(resolved))
            {
                item.UpdateCover(resolved);
                await SyncCoverPathToDatabaseAsync(item.SteamAppId, resolved);
            }
        }
        catch
        {
            // Prefetch log captures bulk failures; per-tile resolve is best-effort.
        }
    }

    private async Task SyncCoverPathToDatabaseAsync(int appId, string coverPath)
    {
        var game = await _database.GetGameAsync(appId);
        if (game is not null && !string.Equals(game.CoverCachePath, coverPath, StringComparison.OrdinalIgnoreCase))
        {
            await _database.UpsertGameAsync(game with { CoverCachePath = coverPath });
        }
    }

    private async Task RefreshAllCoverTilesAsync() => await HydrateCoverTilesAsync();

    private static bool TryParseCoverAppId(string operationId, out int appId)
    {
        appId = 0;
        if (!operationId.StartsWith("cover-", StringComparison.Ordinal))
        {
            return false;
        }

        var suffix = operationId["cover-".Length..];
        return int.TryParse(suffix, out appId) && appId != 0;
    }

    private async Task RefreshCoverTileAsync(int appId)
    {
        if (Games.Count == 0)
        {
            return;
        }

        var index = -1;
        for (var i = 0; i < Games.Count; i++)
        {
            if (Games[i].SteamAppId == appId)
            {
                index = i;
                break;
            }
        }

        if (index < 0)
        {
            return;
        }

        await HydrateCoverTileAsync(Games[index]);
        LibraryUpdated?.Invoke(this, EventArgs.Empty);
    }
}
