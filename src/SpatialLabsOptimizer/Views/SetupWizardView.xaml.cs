using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using SpatialLabsOptimizer.ViewModels;

namespace SpatialLabsOptimizer.Views;

public sealed partial class SetupWizardView : Page
{
    private SetupWizardViewModel? _viewModel;

    public SetupWizardViewModel? ViewModel => _viewModel;

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
            vm.PropertyChanged += ViewModel_PropertyChanged;
            await vm.LoadAsync();
            SyncFromViewModel();
            DisclaimerCheck.IsChecked = vm.DisclaimerAccepted;
            OfflineCheck.IsChecked = vm.OfflineOnboarding;
            DisplayPicker.SetCatalog(vm.DisplayCatalog);
            DisplayPicker.SelectionChanged += DisplayPicker_SelectionChanged;
            if (!string.IsNullOrWhiteSpace(vm.DetectedDisplayId))
            {
                DisplayPicker.SelectProfileById(vm.DetectedDisplayId);
            }

            if (!string.IsNullOrWhiteSpace(vm.MuxWarning))
            {
                MuxInfoBar.Message = vm.MuxWarning;
                MuxInfoBar.IsOpen = true;
            }
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            DisplayPicker.SelectionChanged -= DisplayPicker_SelectionChanged;
        }

        base.OnNavigatedFrom(e);
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        SyncFromViewModel();
    }

    private void SyncFromViewModel()
    {
        if (_viewModel is null)
        {
            return;
        }

        StatusBlock.Text = _viewModel.Status;
        ViewingDistanceBlock.Text = _viewModel.ViewingDistanceTip;
        BenchmarkResultBlock.Text = _viewModel.BenchmarkResult;
        BenchmarkButton.IsEnabled = !_viewModel.IsBenchmarkRunning;
        InstallProgressBar.Value = _viewModel.InstallProgress;
        InstallProgressBar.Visibility = _viewModel.IsInstallRunning
            ? Microsoft.UI.Xaml.Visibility.Visible
            : Microsoft.UI.Xaml.Visibility.Collapsed;
        InstallLogList.Visibility = _viewModel.IsInstallRunning &&
            (_viewModel.RequiredTools.Count == 0 || _viewModel.CurrentStep < 1)
            ? Microsoft.UI.Xaml.Visibility.Visible
            : Microsoft.UI.Xaml.Visibility.Collapsed;
        InstallLogList.ItemsSource = _viewModel.InstallLog;
        RequiredToolsPanel.ItemsSource = _viewModel.RequiredTools;
        var showTools = _viewModel.CurrentStep >= 1 && _viewModel.RequiredTools.Count > 0
            ? Microsoft.UI.Xaml.Visibility.Visible
            : Microsoft.UI.Xaml.Visibility.Collapsed;
        RequiredToolsPanel.Visibility = showTools;
        RequiredToolsHeader.Visibility = showTools;

        if (_viewModel.CurrentStep >= 3 && _viewModel.ReadinessScore > 0)
        {
            ViewingDistanceBlock.Text = $"{_viewModel.ReadinessSummary} ({_viewModel.ReadinessScore}/100)";
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

    private async void Continue_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
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
        await _viewModel.NextAsync();
        if (!string.IsNullOrWhiteSpace(_viewModel.DetectedDisplayId))
        {
            DisplayPicker.SelectProfileById(_viewModel.DetectedDisplayId);
        }

        if (_viewModel.SelectedDisplay is not null)
        {
            ViewingDistanceCoach.SetProfile(_viewModel.SelectedDisplay.Id);
        }

        SyncFromViewModel();
    }

    private async void DisplayPicker_SelectionChanged(object? sender, EventArgs e)
    {
        if (_viewModel is null || DisplayPicker.SelectedDisplay is null)
        {
            return;
        }

        _viewModel.SelectedDisplay = DisplayPicker.SelectedDisplay;
        ViewingDistanceBlock.Text = _viewModel.ViewingDistanceTip =
            App.Services.GetRequiredService<Infrastructure.Displays.ViewingDistanceCoach>()
                .GetTipForProfile(DisplayPicker.SelectedDisplay.Id);
        ViewingDistanceCoach.SetProfile(DisplayPicker.SelectedDisplay.Id);
        await _viewModel.RefreshRequiredToolsAsync(DisplayPicker.SelectedDisplay);
        SyncFromViewModel();
    }

    private void ViewingDistanceCoach_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ViewingDistanceCoach.Visibility = ViewingDistanceCoach.Visibility ==
            Microsoft.UI.Xaml.Visibility.Visible
            ? Microsoft.UI.Xaml.Visibility.Collapsed
            : Microsoft.UI.Xaml.Visibility.Visible;
    }

    private async void Benchmark_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        await _viewModel.RunBenchmarkAsync();
        SyncFromViewModel();
    }
}
