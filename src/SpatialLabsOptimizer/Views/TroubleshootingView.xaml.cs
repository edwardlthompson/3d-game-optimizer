using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using SpatialLabsOptimizer.ViewModels;

namespace SpatialLabsOptimizer.Views;

public sealed partial class TroubleshootingView : Page
{
    private TroubleshootingViewModel? _viewModel;

    public TroubleshootingView()
    {
        InitializeComponent();
        Loaded += TroubleshootingView_Loaded;
    }

    private async void TroubleshootingView_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel = App.Services.GetRequiredService<TroubleshootingViewModel>();
        DataContext = _viewModel;
        await _viewModel.LoadAsync();
        V2Panel.Visibility = _viewModel.V2Enabled
            ? Microsoft.UI.Xaml.Visibility.Visible
            : Microsoft.UI.Xaml.Visibility.Collapsed;
        ExportItemsList.ItemsSource = _viewModel.ExportItems;
        DryRunAppIdBox.Text = _viewModel.DryRunAppId;
    }

    private async void Export_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        await _viewModel.ExportDiagnosticsAsync();
        ExportPathBlock.Text = _viewModel.StatusText;
    }

    private async void DryRun_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.DryRunAppId = DryRunAppIdBox.Text;
        await _viewModel.DryRunAsync();
        ExportPathBlock.Text = _viewModel.StatusText;
    }

    private async void SeedExport_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        await _viewModel.SeedExportAsync();
        ExportPathBlock.Text = _viewModel.StatusText;
    }

    private async void WorkshopImport_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.WorkshopUrl = WorkshopUrlBox.Text;
        await _viewModel.WorkshopImportAsync();
        V2StatusBlock.Text = _viewModel.V2StatusText;
    }

    private async void LanExport_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        await _viewModel.LanExportAsync();
        V2StatusBlock.Text = _viewModel.V2StatusText;
    }

    private async void LanPresetExport_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        await _viewModel.LanPresetExportAsync();
        V2StatusBlock.Text = _viewModel.V2StatusText;
    }

    private async void HybridSession_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        await _viewModel.HybridSessionAsync();
        V2StatusBlock.Text = _viewModel.V2StatusText;
    }
}
