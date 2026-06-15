using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using SpatialLabsOptimizer.Infrastructure.Library;
using SpatialLabsOptimizer.ViewModels;

namespace SpatialLabsOptimizer.Views;

public sealed partial class GameLibraryView : Page
{
    public GameLibraryViewModel ViewModel { get; private set; } = null!;

    public GameLibraryView()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is not GameLibraryViewModel vm)
        {
            return;
        }

        ViewModel = vm;
        Bindings.Update();
        if (vm.IsLibraryLoaded)
        {
            await vm.LoadFromCacheAsync();
        }
        else
        {
            await vm.LoadAsync();
        }
    }

    private void PlayContext_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem item)
        {
            return;
        }

        if (GamesGrid.SelectedItem is not GameLibraryItemViewModel &&
            item.DataContext is GameLibraryItemViewModel contextItem)
        {
            ViewModel.SelectedGame = contextItem;
        }
        else if (GamesGrid.SelectedItem is GameLibraryItemViewModel gridItem)
        {
            ViewModel.SelectedGame = gridItem;
        }

        if (item.Tag is string tag && tag == "vr")
        {
            ViewModel.PlayVrCommand.Execute(null);
        }
        else
        {
            ViewModel.PlayCommand.Execute(null);
        }
    }
}
