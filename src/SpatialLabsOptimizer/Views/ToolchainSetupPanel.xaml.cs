using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SpatialLabsOptimizer.ViewModels;
using WinRT.Interop;

namespace SpatialLabsOptimizer.Views;

public sealed partial class ToolchainSetupPanel : UserControl
{
    private ToolchainSetupViewModel? _viewModel;

    public ToolchainSetupPanel()
    {
        InitializeComponent();
    }

    public void Bind(ToolchainSetupViewModel viewModel)
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            DisplayPicker.SelectionChanged -= DisplayPicker_SelectionChanged;
        }

        _viewModel = viewModel;
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
        DisplayPicker.SetCatalog(viewModel.DisplayCatalog);
        DisplayPicker.SelectionChanged += DisplayPicker_SelectionChanged;
        if (!string.IsNullOrWhiteSpace(viewModel.DetectedDisplayId))
        {
            DisplayPicker.SelectProfileById(viewModel.DetectedDisplayId);
        }

        SyncFromViewModel();
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        => SyncFromViewModel();

    private void SyncFromViewModel()
    {
        if (_viewModel is null)
        {
            return;
        }

        ViewingDistanceBlock.Text = _viewModel.ViewingDistanceTip;
        SpatialLabsNoteBlock.Text = _viewModel.SpatialLabsNote;
        StatusBlock.Text = _viewModel.Status;
        RequiredToolsPanel.ItemsSource = _viewModel.RequiredTools;
        InstallProgressBar.Value = _viewModel.InstallProgress;
        InstallProgressBar.Visibility = _viewModel.IsInstallRunning
            ? Visibility.Visible
            : Visibility.Collapsed;
        InstallLogList.Visibility = _viewModel.IsInstallRunning
            ? Visibility.Visible
            : Visibility.Collapsed;
        InstallLogList.ItemsSource = _viewModel.InstallLog;
        InstallAllButton.IsEnabled = _viewModel.CanInstall;
    }

    private void DisclaimerCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.DisclaimerAccepted = DisclaimerCheck.IsChecked == true;
        }
    }

    private async void DisplayPicker_SelectionChanged(object? sender, EventArgs e)
    {
        if (_viewModel is null || DisplayPicker.SelectedDisplay is null)
        {
            return;
        }

        await _viewModel.OnDisplaySelectedAsync(DisplayPicker.SelectedDisplay);
        SyncFromViewModel();
    }

    private async void InstallAll_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.InstallAllMissingCommand.Execute(null);
        SyncFromViewModel();
        await Task.CompletedTask;
    }

    private async void ToolInstall_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel is null || sender is not FrameworkElement { DataContext: ToolInstallItemViewModel item })
        {
            return;
        }

        await _viewModel.InstallToolAsync(item.ToolId);
        SyncFromViewModel();
    }

    private async void ToolGuide_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel is null || sender is not FrameworkElement { DataContext: ToolInstallItemViewModel item })
        {
            return;
        }

        await _viewModel.OpenToolGuideAsync(item.ToolId);
        SyncFromViewModel();
    }

    private async void ToolLocate_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel is null || sender is not FrameworkElement { DataContext: ToolInstallItemViewModel item })
        {
            return;
        }

        var path = await PickFolderAsync();
        if (path is not null)
        {
            await _viewModel.RegisterToolInstallPathAsync(item.ToolId, path);
            SyncFromViewModel();
        }
    }

    private async void ToolVerify_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel is null || sender is not FrameworkElement { DataContext: ToolInstallItemViewModel item })
        {
            return;
        }

        await _viewModel.VerifyToolInstallAsync(item.ToolId);
        SyncFromViewModel();
    }

    private static async Task<string?> PickFolderAsync()
    {
        var picker = new Windows.Storage.Pickers.FolderPicker();
        picker.FileTypeFilter.Add("*");
        if (App.Current is App app && app.PrimaryWindow is not null)
        {
            var hwnd = WindowNative.GetWindowHandle(app.PrimaryWindow);
            InitializeWithWindow.Initialize(picker, hwnd);
        }

        var folder = await picker.PickSingleFolderAsync();
        return folder?.Path;
    }
}
