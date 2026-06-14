using Microsoft.Extensions.DependencyInjection;
using SpatialLabsOptimizer.Infrastructure.Settings;
using SpatialLabsOptimizer.Infrastructure.Updates;
using Windows.System;

namespace SpatialLabsOptimizer.Views;

public sealed partial class AboutView : Microsoft.UI.Xaml.Controls.Page
{
    private UpdateCheckResult? _lastResult;
    private bool _loaded;

    public AboutView()
    {
        InitializeComponent();
        Loaded += AboutView_Loaded;
    }

    private async void AboutView_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var version = ProductVersionReader.ReadCurrentVersion();
        VersionBlock.Text = $"Version {version}";

        var prefs = App.Services.GetRequiredService<UserPreferencesService>();
        var detector = App.Services.GetRequiredService<InstallArtifactDetector>();
        var artifactType = await prefs.GetInstallArtifactTypeAsync(detector);
        InstallTypeBlock.Text = $"Installed as: {DescribeInstallType(artifactType)}";

        var interval = await prefs.GetUpdateCheckIntervalAsync();
        UpdateIntervalCombo.SelectedIndex = interval switch
        {
            UpdateCheckInterval.Off => 0,
            UpdateCheckInterval.Startup => 1,
            UpdateCheckInterval.Daily => 2,
            _ => 3
        };

        InstallTypeOverrideCombo.SelectedIndex = 0;
        _lastResult = await prefs.GetCachedUpdateResultAsync();
        RenderUpdateStatus(_lastResult);
        RetryUpdateInfoBar.IsOpen = await prefs.GetUpdateRestartPendingAsync();
        _loaded = true;
    }

    private async void UpdateInterval_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
    {
        if (!_loaded || UpdateIntervalCombo.SelectedItem is not Microsoft.UI.Xaml.Controls.ComboBoxItem item ||
            item.Tag is not string tag ||
            !Enum.TryParse<UpdateCheckInterval>(tag, out var interval))
        {
            return;
        }

        var prefs = App.Services.GetRequiredService<UserPreferencesService>();
        await prefs.SetUpdateCheckIntervalAsync(interval);
    }

    private async void InstallTypeOverride_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
    {
        if (!_loaded || InstallTypeOverrideCombo.SelectedItem is not Microsoft.UI.Xaml.Controls.ComboBoxItem item)
        {
            return;
        }

        var prefs = App.Services.GetRequiredService<UserPreferencesService>();
        if (item.Tag is string tag && !string.IsNullOrWhiteSpace(tag) &&
            Enum.TryParse<InstallArtifactType>(tag, out var type))
        {
            await prefs.SetInstallArtifactTypeAsync(type);
            InstallTypeBlock.Text = $"Installed as: {DescribeInstallType(type)} (override)";
            return;
        }

        var detector = App.Services.GetRequiredService<InstallArtifactDetector>();
        var detected = detector.Detect();
        await prefs.SetInstallArtifactTypeAsync(detected);
        InstallTypeBlock.Text = $"Installed as: {DescribeInstallType(detected)}";
    }

    private async void CheckUpdate_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        UpdateStatusBlock.Text = "Checking…";
        var scheduler = App.Services.GetRequiredService<UpdateScheduler>();
        _lastResult = await scheduler.CheckNowAsync();
        RenderUpdateStatus(_lastResult);
    }

    private async void ApplyUpdate_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_lastResult is null || !_lastResult.IsUpdateAvailable)
        {
            return;
        }

        await RunUpdateApplyAsync();
    }

    private async void RetryUpdate_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_lastResult is null)
        {
            _lastResult = await App.Services.GetRequiredService<UserPreferencesService>().GetCachedUpdateResultAsync();
        }

        if (_lastResult is null)
        {
            UpdateStatusBlock.Text = "No staged update metadata — check for updates first.";
            return;
        }

        await RunUpdateApplyAsync();
    }

    private async Task RunUpdateApplyAsync()
    {
        ApplyUpdateButton.IsEnabled = false;
        RetryUpdateButton.IsEnabled = false;
        UpdateStatusBlock.Text = "Applying update…";
        try
        {
            var apply = App.Services.GetRequiredService<UpdateApplyService>();
            await apply.ApplyUpdateAsync(_lastResult!);
        }
        catch (Exception ex)
        {
            UpdateStatusBlock.Text = ex.Message;
            ApplyUpdateButton.IsEnabled = _lastResult?.IsUpdateAvailable == true;
            RetryUpdateButton.IsEnabled = true;
        }
    }

    private async void ReleaseNotesLink_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_lastResult?.ReleasePageUrl is not null)
        {
            await Launcher.LaunchUriAsync(new Uri(_lastResult.ReleasePageUrl));
        }
    }

    private void RenderUpdateStatus(UpdateCheckResult? result)
    {
        if (result is null)
        {
            UpdateStatusBlock.Text = "No update information yet.";
            ApplyUpdateButton.IsEnabled = false;
            ReleaseNotesLink.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            return;
        }

        if (!string.IsNullOrWhiteSpace(result.ErrorMessage) && result.LatestVersion is null)
        {
            UpdateStatusBlock.Text = result.ErrorMessage;
            ApplyUpdateButton.IsEnabled = false;
            ReleaseNotesLink.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            return;
        }

        if (result.IsUpdateAvailable)
        {
            UpdateStatusBlock.Text = string.IsNullOrWhiteSpace(result.ErrorMessage)
                ? $"Update available: v{result.LatestVersion} (you have v{result.CurrentVersion})."
                : $"Update available: v{result.LatestVersion}. {result.ErrorMessage}";
            ApplyUpdateButton.IsEnabled = !string.IsNullOrWhiteSpace(result.DownloadUrl);
            ReleaseNotesLink.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            return;
        }

        UpdateStatusBlock.Text = $"Up to date (v{result.CurrentVersion}).";
        ApplyUpdateButton.IsEnabled = false;
        ReleaseNotesLink.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
    }

    private static string DescribeInstallType(InstallArtifactType type) => type switch
    {
        InstallArtifactType.Msix => "MSIX",
        InstallArtifactType.Msi => "MSI",
        _ => "Portable (zip)"
    };
}
