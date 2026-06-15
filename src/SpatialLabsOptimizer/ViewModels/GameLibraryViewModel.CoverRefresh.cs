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

        if ((report.OperationId == "artwork-prefetch" || report.OperationId == "metadata-prefetch") && report.IsComplete)
        {
            RunOnUiThread(() => _ = RefreshAllCoverTilesAsync());
        }
    }

    private async Task RefreshAllCoverTilesAsync()
    {
        if (Games.Count == 0)
        {
            return;
        }

        foreach (var item in Games)
        {
            var game = await _database.GetGameAsync(item.SteamAppId);
            if (game?.CoverCachePath is not null)
            {
                item.UpdateCover(game.CoverCachePath);
            }
        }

        LibraryUpdated?.Invoke(this, EventArgs.Empty);
    }

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

        var game = await _database.GetGameAsync(appId);
        if (game is null || string.IsNullOrWhiteSpace(game.CoverCachePath))
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

        Games[index].UpdateCover(game.CoverCachePath);
        LibraryUpdated?.Invoke(this, EventArgs.Empty);
    }
}
