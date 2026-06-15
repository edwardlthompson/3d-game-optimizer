using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Library;
using SpatialLabsOptimizer.Infrastructure.Settings;
namespace SpatialLabsOptimizer.ViewModels;

public sealed partial class GameLibraryViewModel
{
    private async Task LoadLibraryPrefsAsync()
    {
        var prefs = await _preferences.GetLibraryUiPrefsAsync();
        _sortMode = Enum.TryParse<LibrarySortMode>(prefs.SortMode, true, out var sort) ? sort : LibrarySortMode.Quality;
        _smartCollection = Enum.TryParse<SmartCollectionMode>(prefs.SmartCollection, true, out var smart)
            ? smart
            : SmartCollectionMode.None;
        _showFavoritesOnly = prefs.ShowFavoritesOnly;
        _showLocalOnly = prefs.ShowLocalOnly;
        _showWhyNotReady = prefs.ShowWhyNotReady;
        _playlistName = prefs.LastPlaylistName;
        OnPropertyChanged(nameof(SortMode));
        OnPropertyChanged(nameof(SmartCollection));
        OnPropertyChanged(nameof(ShowFavoritesOnly));
        OnPropertyChanged(nameof(ShowLocalOnly));
        OnPropertyChanged(nameof(ShowWhyNotReady));
        OnPropertyChanged(nameof(PlaylistName));
    }

    private void ScheduleSaveLibraryPrefs()
    {
        if (!_libraryPrefsLoaded)
        {
            return;
        }

        _prefsSaveCts?.Cancel();
        _prefsSaveCts = new CancellationTokenSource();
        var token = _prefsSaveCts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(300, token);
                await _preferences.SetLibraryUiPrefsAsync(new LibraryUiPrefs(
                    SortMode: SortMode.ToString(),
                    SmartCollection: SmartCollection.ToString(),
                    ShowFavoritesOnly: ShowFavoritesOnly,
                    ShowLocalOnly: ShowLocalOnly,
                    ShowWhyNotReady: ShowWhyNotReady,
                    LastPlaylistName: PlaylistName));
            }
            catch (OperationCanceledException)
            {
                // Debounce superseded.
            }
        }, token);
    }
}
