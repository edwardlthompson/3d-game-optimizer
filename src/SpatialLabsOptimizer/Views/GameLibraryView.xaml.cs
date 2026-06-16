using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using SpatialLabsOptimizer.Controls;
using SpatialLabsOptimizer.Infrastructure.Library;
using SpatialLabsOptimizer.ViewModels;

namespace SpatialLabsOptimizer.Views;

public sealed partial class GameLibraryView : Page
{
    private const double RecentPanelMinHeight = 80;
    private const double RecentPanelMaxHeight = 480;
    private const int RecentPanelRowIndex = 2;

    private bool _draggingRecentPanel;
    private double _dragStartY;
    private double _dragStartPanelHeight;

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

    private async void GamesGrid_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not GameLibraryItemViewModel item)
        {
            return;
        }

        await ShowGameDetailAsync(item);
    }

    private async void OpenDetailContext_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (ResolveContextItem(sender) is not { } item)
        {
            return;
        }

        await ShowGameDetailAsync(item);
    }

    private async Task ShowGameDetailAsync(GameLibraryItemViewModel item)
    {
        ViewModel.SelectedGame = item;
        var dialog = new GameDetailDialog(ViewModel) { XamlRoot = XamlRoot };
        await dialog.ShowAsync();
    }

    private void PlayContext_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem item)
        {
            return;
        }

        if (ResolveContextItem(sender) is not { } game)
        {
            return;
        }

        ViewModel.SelectedGame = game;
        if (item.Tag is string tag && tag == "vr")
        {
            ViewModel.PlayVrCommand.Execute(null);
        }
        else
        {
            ViewModel.PlayCommand.Execute(null);
        }
    }

    private GameLibraryItemViewModel? ResolveContextItem(object sender)
    {
        if (GamesGrid.SelectedItem is GameLibraryItemViewModel selected)
        {
            return selected;
        }

        if (sender is MenuFlyoutItem { DataContext: GameLibraryItemViewModel contextItem })
        {
            return contextItem;
        }

        return null;
    }

    private void RecentLaunchGrip_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not UIElement grip)
        {
            return;
        }

        _draggingRecentPanel = true;
        _dragStartY = e.GetCurrentPoint(LibraryContentGrid).Position.Y;
        _dragStartPanelHeight = LibraryContentGrid.RowDefinitions[RecentPanelRowIndex].ActualHeight;
        grip.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void RecentLaunchGrip_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_draggingRecentPanel)
        {
            return;
        }

        var delta = _dragStartY - e.GetCurrentPoint(LibraryContentGrid).Position.Y;
        var nextHeight = Math.Clamp(_dragStartPanelHeight + delta, RecentPanelMinHeight, RecentPanelMaxHeight);
        LibraryContentGrid.RowDefinitions[RecentPanelRowIndex].Height = new GridLength(nextHeight);
        e.Handled = true;
    }

    private void RecentLaunchGrip_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_draggingRecentPanel)
        {
            return;
        }

        _draggingRecentPanel = false;
        if (sender is UIElement grip)
        {
            grip.ReleasePointerCapture(e.Pointer);
        }

        e.Handled = true;
    }
}
