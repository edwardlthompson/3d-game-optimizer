using Microsoft.Extensions.DependencyInjection;
using SpatialLabsOptimizer.Infrastructure.Pcvr;

namespace SpatialLabsOptimizer.Views;

public sealed partial class CommandPaletteView : Microsoft.UI.Xaml.Controls.Page
{
    public CommandPaletteView()
    {
        InitializeComponent();
        RefreshResults("");
    }

    private void SearchBox_TextChanged(object sender, Microsoft.UI.Xaml.Controls.TextChangedEventArgs e)
    {
        RefreshResults(SearchBox.Text);
    }

    private async void ResultsList_ItemClick(object sender, Microsoft.UI.Xaml.Controls.ItemClickEventArgs e)
    {
        if (e.ClickedItem is CommandPaletteEntry entry && ShellPage.Current is not null)
        {
            await ShellPage.Current.ExecuteCommandAsync(entry.Id);
        }
    }

    private void RefreshResults(string query)
    {
        var palette = App.Services.GetRequiredService<CommandPaletteService>();
        ResultsList.ItemsSource = palette.Search(query);
    }
}
