using Microsoft.Extensions.DependencyInjection;
using SpatialLabsOptimizer.ViewModels;

namespace SpatialLabsOptimizer.Views;

public sealed partial class GlossaryView : Microsoft.UI.Xaml.Controls.Page
{
    public GlossaryView()
    {
        InitializeComponent();
        Loaded += GlossaryView_Loaded;
    }

    private async void GlossaryView_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var viewModel = App.Services.GetRequiredService<GlossaryViewModel>();
        DataContext = viewModel;
        await viewModel.LoadAsync();
        GlossaryItems.ItemsSource = viewModel.Entries;
        LoadErrorBlock.Text = viewModel.LoadError;
        LoadErrorBlock.Visibility = string.IsNullOrWhiteSpace(viewModel.LoadError)
            ? Microsoft.UI.Xaml.Visibility.Collapsed
            : Microsoft.UI.Xaml.Visibility.Visible;
    }
}
