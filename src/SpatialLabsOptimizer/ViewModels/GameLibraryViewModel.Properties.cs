using System.Windows.Input;
using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Library;

namespace SpatialLabsOptimizer.ViewModels;

public sealed partial class GameLibraryViewModel
{
    public event EventHandler? LibraryUpdated;

    public IReadOnlyList<GameLibraryItemViewModel> Games
    {
        get => _games;
        set => SetProperty(ref _games, value);
    }

    public IReadOnlyList<string> PlaylistNames
    {
        get => _playlistNames;
        set => SetProperty(ref _playlistNames, value);
    }

    public IReadOnlyList<RecentLaunchItemViewModel> RecentLaunches
    {
        get => _recentLaunches;
        set => SetProperty(ref _recentLaunches, value);
    }

    public LibrarySortMode SortMode
    {
        get => _sortMode;
        set
        {
            if (SetProperty(ref _sortMode, value))
            {
                ApplySort();
                ScheduleSaveLibraryPrefs();
            }
        }
    }

    public SmartCollectionMode SmartCollection
    {
        get => _smartCollection;
        set
        {
            if (SetProperty(ref _smartCollection, value))
            {
                _ = LoadAsync();
                ScheduleSaveLibraryPrefs();
            }
        }
    }

    public string WarmStartStatus
    {
        get => _warmStartStatus;
        set => SetProperty(ref _warmStartStatus, value);
    }

    public string PreferredOutput
    {
        get => _preferredOutput;
        set => SetProperty(ref _preferredOutput, value);
    }

    public string PlaylistName
    {
        get => _playlistName;
        set => SetProperty(ref _playlistName, value);
    }

    public string CompatibilityNote
    {
        get => _compatibilityNote;
        set => SetProperty(ref _compatibilityNote, value);
    }

    public string SelectedPresetFreshness
    {
        get => _selectedPresetFreshness;
        set => SetProperty(ref _selectedPresetFreshness, value);
    }

    public string WhyNotReadyHint
    {
        get => _whyNotReadyHint;
        set => SetProperty(ref _whyNotReadyHint, value);
    }

    public bool ShowFavoritesOnly
    {
        get => _showFavoritesOnly;
        set
        {
            if (SetProperty(ref _showFavoritesOnly, value))
            {
                _ = LoadAsync();
                ScheduleSaveLibraryPrefs();
            }
        }
    }

    public bool ShowLocalOnly
    {
        get => _showLocalOnly;
        set
        {
            if (SetProperty(ref _showLocalOnly, value))
            {
                _ = LoadAsync();
                ScheduleSaveLibraryPrefs();
            }
        }
    }

    public bool ShowWhyNotReady
    {
        get => _showWhyNotReady;
        set
        {
            if (SetProperty(ref _showWhyNotReady, value))
            {
                _ = LoadAsync();
                ScheduleSaveLibraryPrefs();
            }
        }
    }

    public int GridColumns => _responsive.CurrentColumns;

    public GameLibraryItemViewModel? SelectedGame { get; set; }

    public ICommand PlayCommand { get; }
    public ICommand PlayVrCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand PinCommand { get; }
    public ICommand UnpinCommand { get; }
    public ICommand QueueCommand { get; }
    public ICommand PlayNextCommand { get; }
    public ICommand ToggleFavoriteCommand { get; }
    public ICommand SavePlaylistCommand { get; }
    public ICommand LoadPlaylistCommand { get; }
    public ICommand SaveOutputCommand { get; }
    public ICommand SaveCompatibilityNoteCommand { get; }
    public ICommand RefreshPresetCommand { get; }
}
