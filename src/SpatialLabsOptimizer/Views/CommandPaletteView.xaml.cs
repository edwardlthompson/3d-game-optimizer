using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using SpatialLabsOptimizer.Infrastructure.Pcvr;
using SpatialLabsOptimizer.ViewModels;

namespace SpatialLabsOptimizer.Views;

public sealed partial class CommandPaletteView : Page
{
    public CommandPaletteViewModel ViewModel { get; private set; } = null!;

    public CommandPaletteView()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is not CommandPaletteViewModel vm)
        {
            return;
        }

        ViewModel = vm;
        Bindings.Update();
        ViewModel.Search("");
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox box)
        {
            ViewModel.Search(box.Text);
        }
    }

    private async void ResultsList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is CommandPaletteEntry entry && ShellPage.Current is not null)
        {
            await ShellPage.Current.ExecuteCommandAsync(entry.Id);
        }
    }
}
