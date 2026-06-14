using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using SpatialLabsOptimizer.ViewModels;

namespace SpatialLabsOptimizer.Views;

public sealed partial class SetupWizardView : Page
{
    private SetupWizardViewModel? _viewModel;

    public SetupWizardView()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is SetupWizardViewModel vm)
        {
            _viewModel = vm;
            await vm.LoadAsync();
            StatusBlock.Text = vm.Status;
            DisclaimerCheck.IsChecked = vm.DisclaimerAccepted;
            DisplayPicker.SetCatalog(vm.DisplayCatalog);
            if (!string.IsNullOrWhiteSpace(vm.MuxWarning))
            {
                MuxInfoBar.Message = vm.MuxWarning;
                MuxInfoBar.IsOpen = true;
            }
        }
    }

    private void DisclaimerCheck_Changed(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.DisclaimerAccepted = DisclaimerCheck.IsChecked == true;
        }
    }

    private void Continue_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        if (_viewModel.CurrentStep == 0)
        {
            DisplayPicker.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            DisclaimerPanel.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        }

        _viewModel.SelectedDisplay = DisplayPicker.SelectedDisplay;
        _viewModel.NextCommand.Execute(null);
        StatusBlock.Text = _viewModel.Status;
        ViewingDistanceBlock.Text = _viewModel.ViewingDistanceTip;
    }

    private void Benchmark_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel?.RunBenchmarkCommand.Execute(null);
    }
}
