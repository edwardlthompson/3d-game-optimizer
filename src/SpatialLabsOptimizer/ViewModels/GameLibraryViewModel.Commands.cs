using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Library;

namespace SpatialLabsOptimizer.ViewModels;

public sealed partial class GameLibraryViewModel
{
    public async Task SaveCompatibilityNoteAsync()
    {
        if (SelectedGame is null)
        {
            return;
        }

        await _intelligence.SaveCompatibilityNoteAsync(SelectedGame.SteamAppId, CompatibilityNote);
    }

    public async Task RefreshSelectedPresetAsync()
    {
        if (SelectedGame is null)
        {
            return;
        }

        await _presets.CachePresetAsync(SelectedGame.SteamAppId);
        SelectedPresetFreshness = await _intelligence.GetPresetFreshnessLabelAsync(SelectedGame.SteamAppId);
    }

    public async Task PinSelectedAsync()
    {
        if (SelectedGame is null)
        {
            return;
        }

        var pinned = (await _pinnedShelf.GetPinnedAppIdsAsync()).ToList();
        if (!pinned.Contains(SelectedGame.SteamAppId))
        {
            pinned.Add(SelectedGame.SteamAppId);
            await _pinnedShelf.SetPinnedAppIdsAsync(pinned);
            await LoadAsync();
        }
    }

    public async Task UnpinSelectedAsync()
    {
        if (SelectedGame is null)
        {
            return;
        }

        await _pinnedShelf.RemovePinnedAppIdAsync(SelectedGame.SteamAppId);
        await LoadAsync();
    }

    public async Task EnqueueSelectedAsync()
    {
        if (SelectedGame is null)
        {
            return;
        }

        _playQueue.Enqueue(SelectedGame.SteamAppId);
        await LoadAsync();
    }

    public async Task PlayNextAsync()
    {
        if (!_playQueue.TryDequeue(out var appId))
        {
            return;
        }

        var title = Games.FirstOrDefault(g => g.SteamAppId == appId)?.Title;
        if (title is null)
        {
            var game = await _database.GetGameAsync(appId);
            title = game?.Title ?? $"App {appId}";
        }

        _shell.ShowLaunchOverlay = true;
        _shell.LaunchGameTitle = title;
        await _playIn3D.ExecuteAsync(appId);
        _shell.ShowLaunchOverlay = false;
        await LoadAsync();
    }

    public async Task ToggleFavoriteSelectedAsync()
    {
        if (SelectedGame is null)
        {
            return;
        }

        await _database.SetFavoriteAsync(SelectedGame.SteamAppId, !SelectedGame.IsFavorite);
        await LoadAsync();
    }

    public async Task SavePlaylistAsync()
    {
        if (string.IsNullOrWhiteSpace(PlaylistName) || Games.Count == 0)
        {
            return;
        }

        var ids = Games.Select(g => g.SteamAppId).ToList();
        await _playlists.SavePlaylistAsync(PlaylistName.Trim(), ids);
        await LoadAsync();
    }

    public async Task LoadPlaylistAsync()
    {
        if (string.IsNullOrWhiteSpace(PlaylistName))
        {
            return;
        }

        var ids = await _playlists.LoadPlaylistAsync(PlaylistName.Trim());
        foreach (var id in ids)
        {
            _playQueue.Enqueue(id);
        }

        await LoadAsync();
    }

    public async Task SavePreferredOutputAsync()
    {
        if (SelectedGame is null)
        {
            return;
        }

        var existing = await _overrides.GetAsync(SelectedGame.SteamAppId);
        await _overrides.SaveAsync(new GameOverride(
            SelectedGame.SteamAppId,
            existing?.Depth ?? 0.65,
            existing?.Convergence ?? 0.5,
            existing?.PlatformOverride ?? LaunchPlatform.Uevr,
            existing?.SafeLaunch ?? false,
            PreferredOutput));
    }

    private async Task PlaySelectedAsync()
    {
        if (SelectedGame is null)
        {
            return;
        }

        await PlayByAppIdAsync(SelectedGame.SteamAppId, SelectedGame.Title);
    }

    public async Task PlayByAppIdAsync(int appId, string? title = null)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            var game = await _database.GetGameAsync(appId);
            title = game?.Title ?? $"App {appId}";
        }

        _shell.ShowLaunchOverlay = true;
        _shell.LaunchGameTitle = title;
        await _playIn3D.ExecuteAsync(appId);
        _shell.ShowLaunchOverlay = false;
        await LoadAsync();
    }

    private async Task PlayVrSelectedAsync()
    {
        if (SelectedGame is null)
        {
            return;
        }

        _shell.ShowLaunchOverlay = true;
        _shell.LaunchGameTitle = SelectedGame.Title;
        await _playInVr.ExecuteAsync(SelectedGame.SteamAppId);
        _shell.ShowLaunchOverlay = false;
    }
}
