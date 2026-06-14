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
        SortCombo.SelectedIndex = 0;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is GameLibraryViewModel vm)
        {
            _viewModel = vm;
            await vm.LoadAsync();
            BindLibrary();
        }
    }

    private void BindLibrary()
    {
        if (_viewModel is null)
        {
            return;
        }

        GamesGrid.ItemsSource = _viewModel.Games;
        WarmStartBlock.Text = _viewModel.WarmStartStatus;
    }

    private void GamesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_viewModel is not null && GamesGrid.SelectedItem is GameLibraryItemViewModel item)
        {
            _viewModel.SelectedGame = item;
        }
    }

    private async void Refresh_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_viewModel is not null)
        {
            await _viewModel.LoadAsync();
            BindLibrary();
        }
    }

    private void Play_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel?.PlayCommand.Execute(null);
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
            _ => LibrarySortMode.Quality
        };
        BindLibrary();
    }
}
