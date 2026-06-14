using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using SpatialLabsOptimizer.Infrastructure.Library;
using SpatialLabsOptimizer.ViewModels;

namespace SpatialLabsOptimizer.Views;

public sealed partial class GameLibraryView : Page
{
    private GameLibraryViewModel? _viewModel;

    public GameLibraryView()
    {
        InitializeComponent();
        SortCombo.Items.Add("3D Quality");
        SortCombo.Items.Add("Players Online");
        SortCombo.Items.Add("Steam Reviews");
        SortCombo.Items.Add("Name");
        SortCombo.Items.Add("Genre");
        SortCombo.SelectedIndex = 0;
        OutputCombo.SelectedIndex = 0;
        SmartCollectionCombo.Items.Add("None");
        SmartCollectionCombo.Items.Add("Favorites + tier");
        SmartCollectionCombo.Items.Add("Never played in 3D");
        SmartCollectionCombo.Items.Add("Local only");
        SmartCollectionCombo.SelectedIndex = 0;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is GameLibraryViewModel vm)
        {
            if (_viewModel is not null)
            {
                _viewModel.LibraryUpdated -= OnLibraryUpdated;
            }

            _viewModel = vm;
            _viewModel.LibraryUpdated += OnLibraryUpdated;
            await vm.LoadAsync();
            BindLibrary();
        }
    }

    private void OnLibraryUpdated(object? sender, EventArgs e) => BindLibrary();

    private void BindLibrary()
    {
        if (_viewModel is null)
        {
            return;
        }

        GamesGrid.ItemsSource = _viewModel.Games;
        WarmStartBlock.Text = _viewModel.WarmStartStatus;
        FavoritesOnlyCheck.IsChecked = _viewModel.ShowFavoritesOnly;
        LocalOnlyCheck.IsChecked = _viewModel.ShowLocalOnly;
        WhyNotReadyCheck.IsChecked = _viewModel.ShowWhyNotReady;
        WhyNotReadyBlock.Text = _viewModel.WhyNotReadyHint;
        PresetFreshnessBlock.Text = _viewModel.SelectedPresetFreshness;
        CompatibilityNoteBox.Text = _viewModel.CompatibilityNote;
        RecentLaunchesList.ItemsSource = _viewModel.RecentLaunches;
        PlaylistCombo.Items.Clear();
        foreach (var name in _viewModel.PlaylistNames)
        {
            PlaylistCombo.Items.Add(name);
        }
    }

    private async void GamesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_viewModel is not null && GamesGrid.SelectedItem is GameLibraryItemViewModel item)
        {
            _viewModel.SelectedGame = item;
            await _viewModel.SelectGameAsync(item);
            SelectedGamePanel.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            SelectedGameTitle.Text = item.Title;
            OutputCombo.SelectedIndex = _viewModel.PreferredOutput switch
            {
                "Monitor" => 1,
                "Headset" => 2,
                _ => 0
            };
            WhyNotReadyBlock.Text = _viewModel.WhyNotReadyHint;
            PresetFreshnessBlock.Text = _viewModel.SelectedPresetFreshness;
            CompatibilityNoteBox.Text = _viewModel.CompatibilityNote;
        }
        else
        {
            SelectedGamePanel.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        }
    }

    private async void Refresh_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_viewModel is not null)
        {
            await _viewModel.LoadAsync();
        }
    }

    private void Play_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        => _viewModel?.PlayCommand.Execute(null);

    private void PlayVr_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        => _viewModel?.PlayVrCommand.Execute(null);

    private void PlayContext_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_viewModel is null || sender is not MenuFlyoutItem item)
        {
            return;
        }

        if (GamesGrid.SelectedItem is not GameLibraryItemViewModel selected &&
            item.DataContext is GameLibraryItemViewModel contextItem)
        {
            GamesGrid.SelectedItem = contextItem;
            _viewModel.SelectedGame = contextItem;
        }
        else if (GamesGrid.SelectedItem is GameLibraryItemViewModel gridItem)
        {
            _viewModel.SelectedGame = gridItem;
        }

        if (item.Tag is string tag && tag == "vr")
        {
            _viewModel.PlayVrCommand.Execute(null);
        }
        else
        {
            _viewModel.PlayCommand.Execute(null);
        }
    }

    private void Pin_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        => _viewModel?.PinCommand.Execute(null);

    private void Unpin_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        => _viewModel?.UnpinCommand.Execute(null);

    private void Queue_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        => _viewModel?.QueueCommand.Execute(null);

    private void PlayNext_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        => _viewModel?.PlayNextCommand.Execute(null);

    private void Favorite_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        => _viewModel?.ToggleFavoriteCommand.Execute(null);

    private void SaveOutput_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        => _viewModel?.SaveOutputCommand.Execute(null);

    private void SavePlaylist_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.PlaylistName = PlaylistNameBox.Text;
        _viewModel.SavePlaylistCommand.Execute(null);
    }

    private void LoadPlaylist_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.PlaylistName = PlaylistNameBox.Text;
        _viewModel.LoadPlaylistCommand.Execute(null);
    }

    private void PlaylistCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PlaylistCombo.SelectedItem is string name)
        {
            PlaylistNameBox.Text = name;
            if (_viewModel is not null)
            {
                _viewModel.PlaylistName = name;
            }
        }
    }

    private void FavoritesOnly_Changed(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        => ApplyFilterFlags();

    private void Filter_Changed(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        => ApplyFilterFlags();

    private void ApplyFilterFlags()
    {
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.ShowFavoritesOnly = FavoritesOnlyCheck.IsChecked == true;
        _viewModel.ShowLocalOnly = LocalOnlyCheck.IsChecked == true;
        _viewModel.ShowWhyNotReady = WhyNotReadyCheck.IsChecked == true;
    }

    private void SmartCollection_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_viewModel is null || SmartCollectionCombo.SelectedIndex < 0)
        {
            return;
        }

        _viewModel.SmartCollection = SmartCollectionCombo.SelectedIndex switch
        {
            1 => SmartCollectionMode.FavoritesAndTier,
            2 => SmartCollectionMode.NeverPlayedIn3D,
            3 => SmartCollectionMode.LocalOnly,
            _ => SmartCollectionMode.None
        };
    }

    private void SaveNote_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.CompatibilityNote = CompatibilityNoteBox.Text;
        _viewModel.SaveCompatibilityNoteCommand.Execute(null);
    }

    private void RefreshPreset_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        => _viewModel?.RefreshPresetCommand.Execute(null);

    private void OutputCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_viewModel is null || OutputCombo.SelectedItem is not ComboBoxItem item || item.Tag is not string tag)
        {
            return;
        }

        _viewModel.PreferredOutput = tag;
    }

    private void SortCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_viewModel is null || SortCombo.SelectedIndex < 0)
        {
            return;
        }

        _viewModel.SortMode = SortCombo.SelectedIndex switch
        {
            1 => LibrarySortMode.PlayersOnline,
            2 => LibrarySortMode.SteamReviews,
            3 => LibrarySortMode.Name,
            4 => LibrarySortMode.Genre,
            _ => LibrarySortMode.Quality
        };
    }
}
