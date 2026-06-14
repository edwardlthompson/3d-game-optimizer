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
            OfflineCheck.IsChecked = vm.OfflineOnboarding;
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

    private void OfflineCheck_Changed(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        if (OfflineCheck.IsChecked == true)
        {
            _viewModel.UseOfflineOnboardingPath();
        }
        else
        {
            _viewModel.OfflineOnboarding = false;
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
        if (_viewModel.SelectedDisplay is not null)
        {
            ViewingDistanceCoach.SetProfile(_viewModel.SelectedDisplay.Id);
        }

        if (_viewModel.CurrentStep >= 3 && _viewModel.ReadinessScore > 0)
        {
            ViewingDistanceBlock.Text = $"{_viewModel.ReadinessSummary} ({_viewModel.ReadinessScore}/100)";
        }
    }

    private void ViewingDistanceCoach_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ViewingDistanceCoach.Visibility = ViewingDistanceCoach.Visibility ==
            Microsoft.UI.Xaml.Visibility.Visible
            ? Microsoft.UI.Xaml.Visibility.Collapsed
            : Microsoft.UI.Xaml.Visibility.Visible;
    }

    private void Benchmark_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel?.RunBenchmarkCommand.Execute(null);
    }
}
