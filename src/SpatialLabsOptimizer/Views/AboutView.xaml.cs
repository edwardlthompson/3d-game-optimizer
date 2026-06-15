using Microsoft.Extensions.DependencyInjection;
using SpatialLabsOptimizer.Infrastructure.Settings;
using SpatialLabsOptimizer.Infrastructure.Updates;
using SpatialLabsOptimizer.ViewModels;

namespace SpatialLabsOptimizer.Views;

public sealed partial class AboutView : Microsoft.UI.Xaml.Controls.Page
{
    private AboutViewModel? _viewModel;
    private bool _loaded;

    public AboutView()
    {
        InitializeComponent();
        Loaded += AboutView_Loaded;
    }

    private async void AboutView_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel = App.Services.GetRequiredService<AboutViewModel>();
        DataContext = _viewModel;
        await _viewModel.LoadAsync();
        BindFromViewModel();
        InstallTypeOverrideCombo.SelectedIndex = 0;
        _loaded = true;
    }

    private void BindFromViewModel()
    {
        if (_viewModel is null)
        {
            return;
        }

        VersionBlock.Text = _viewModel.VersionText;
        InstallTypeBlock.Text = _viewModel.InstallTypeText;
        UpdateStatusBlock.Text = _viewModel.UpdateStatusText;
        ApplyUpdateButton.IsEnabled = _viewModel.IsApplyEnabled;
        RetryUpdateInfoBar.IsOpen = _viewModel.IsRetryOpen;
        ReleaseNotesLink.Visibility = _viewModel.ShowReleaseNotes
            ? Microsoft.UI.Xaml.Visibility.Visible
            : Microsoft.UI.Xaml.Visibility.Collapsed;
        UpdateIntervalCombo.SelectedIndex = _viewModel.UpdateInterval switch
        {
            UpdateCheckInterval.Off => 0,
            UpdateCheckInterval.Startup => 1,
            UpdateCheckInterval.Daily => 2,
            _ => 3
        };
    }

    private async void UpdateInterval_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
    {
        if (!_loaded || _viewModel is null ||
            UpdateIntervalCombo.SelectedItem is not Microsoft.UI.Xaml.Controls.ComboBoxItem item ||
            item.Tag is not string tag ||
            !Enum.TryParse<UpdateCheckInterval>(tag, out var interval))
        {
            return;
        }

        await _viewModel.SetUpdateIntervalAsync(interval);
    }

    private async void InstallTypeOverride_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
    {
        if (!_loaded || _viewModel is null ||
            InstallTypeOverrideCombo.SelectedItem is not Microsoft.UI.Xaml.Controls.ComboBoxItem item)
        {
            return;
        }

        if (item.Tag is string tag && !string.IsNullOrWhiteSpace(tag) &&
            Enum.TryParse<InstallArtifactType>(tag, out var type))
        {
            await _viewModel.SetInstallTypeOverrideAsync(type);
        }
        else
        {
            await _viewModel.SetInstallTypeOverrideAsync(null);
        }

        InstallTypeBlock.Text = _viewModel.InstallTypeText;
    }

    private async void CheckUpdate_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        await _viewModel.CheckForUpdatesAsync();
        BindFromViewModel();
    }

    private async void ApplyUpdate_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        => await RunUpdateApplyAsync();

    private async void RetryUpdate_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        => await RunUpdateApplyAsync();

    private async Task RunUpdateApplyAsync()
    {
        if (_viewModel is null)
        {
            return;
        }

        ApplyUpdateButton.IsEnabled = false;
        RetryUpdateButton.IsEnabled = false;
        await _viewModel.ApplyUpdateAsync();
        BindFromViewModel();
        RetryUpdateButton.IsEnabled = true;
    }

    private async void ReleaseNotesLink_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_viewModel is not null)
        {
            await _viewModel.OpenReleaseNotesAsync();
        }
    }
}
