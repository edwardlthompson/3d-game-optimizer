using Microsoft.Extensions.DependencyInjection;
using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Launch.Coexistence;
using SpatialLabsOptimizer.Infrastructure.Performance;
using SpatialLabsOptimizer.Infrastructure.Pcvr;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Settings;
using SpatialLabsOptimizer.Infrastructure.Updates;
using SpatialLabsOptimizer.ViewModels;

namespace SpatialLabsOptimizer.Views;

public sealed partial class Global3DSettingsView : Microsoft.UI.Xaml.Controls.Page
{
    private bool _displayLaunchInitialized;

    private Global3DSettingsViewModel? _settingsViewModel;

    public Global3DSettingsView()
    {
        InitializeComponent();
        Loaded += Global3DSettingsView_Loaded;
    }

    private async void Global3DSettingsView_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var prefs = App.Services.GetRequiredService<UserPreferencesService>();
        _settingsViewModel = App.Services.GetRequiredService<Global3DSettingsViewModel>();
        await _settingsViewModel.LoadAsync();
        SafeLaunchToggle.IsOn = _settingsViewModel.SafeLaunch;
        TrainerToggle.IsOn = _settingsViewModel.TrainerCoexistence;
        ModManagerToggle.IsOn = _settingsViewModel.ModManagerCoexistence;
        SimpleModeToggle.IsOn = _settingsViewModel.SimpleMode;
        RefreshDetectedTools();

        var theme = _settingsViewModel.Theme;
        ThemeCombo.SelectedIndex = theme switch
        {
            "light" => 1,
            "dark" => 2,
            _ => 0
        };

        V2Panel.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        IntegrationsExpander.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        var v2Enabled = await FeatureFlags.IsV2EnabledAsync(prefs);
        V2LanCheck.IsChecked = v2Enabled;
        V2HybridCheck.IsChecked = v2Enabled;
        V2EpicGogCheck.IsChecked = v2Enabled;
        RefreshV2RestartNotice(v2Enabled);

        await RefreshHdrNoticeAsync();
        await RefreshDisplayLaunchPickersAsync();

        if (FeatureFlags.V11Enabled)
        {
            SessionToolsPanel.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            SessionExpander.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            var hotkey = App.Services.GetService<StreamerHotkeyService>();
            StreamerHotkeyBlock.Text = hotkey is null
                ? "Streamer hotkey service unavailable."
                : $"Suggested streamer hotkey: {hotkey.Toggle3DHotkey} (register in your broadcast overlay app).";
            var streamFriendly = App.Services.GetService<StreamFriendlyProfileService>();
            if (streamFriendly is not null)
            {
                StreamFriendlyBlock.Text = string.Join(
                    Environment.NewLine + Environment.NewLine,
                    streamFriendly.GetBundles().Select(streamFriendly.FormatBundleForDisplay));
            }

            await RefreshSessionProfilesAsync();
        }

        await RefreshSnapshotsAsync();
    }

    private async void LaunchSafety_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_settingsViewModel is null)
        {
            return;
        }

        _settingsViewModel.SafeLaunch = SafeLaunchToggle.IsOn;
        _settingsViewModel.TrainerCoexistence = TrainerToggle.IsOn;
        _settingsViewModel.ModManagerCoexistence = ModManagerToggle.IsOn;
        _settingsViewModel.SimpleMode = SimpleModeToggle.IsOn;
        await _settingsViewModel.SaveLaunchSafetyAsync();
    }

    private void RefreshDetectedTools_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        => RefreshDetectedTools();

    private void RefreshDetectedTools()
    {
        var coexistence = App.Services.GetRequiredService<ExternalToolCoexistenceService>();
        var detected = coexistence.GetAllRunningExternalTools();
        DetectedToolsText.Text = detected.Count == 0
            ? "Detected external tools: none"
            : $"Detected external tools: {string.Join(", ", detected)}";
    }

    private async void V2Feature_Changed(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var wantsV2 = V2LanCheck.IsChecked == true ||
                      V2HybridCheck.IsChecked == true ||
                      V2EpicGogCheck.IsChecked == true;
        var prefs = App.Services.GetRequiredService<UserPreferencesService>();
        await prefs.SetV2ExperimentalAsync(wantsV2);
        RefreshV2RestartNotice(wantsV2);
    }

    private void RefreshV2RestartNotice(bool wantsV2)
    {
        if (FeatureFlags.V2EnabledFromEnvironment)
        {
            V2RestartNotice.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            return;
        }

        var mismatch = wantsV2 != FeatureFlags.V2RegisteredAtStartup;
        V2RestartNotice.Visibility = mismatch
            ? Microsoft.UI.Xaml.Visibility.Visible
            : Microsoft.UI.Xaml.Visibility.Collapsed;
        V2RestartNotice.Text = mismatch
            ? "Restart the app to apply integration changes."
            : string.Empty;
    }

    private async void SaveOverride_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (!int.TryParse(OverrideAppIdBox.Text?.Trim(), out var appId) || appId <= 0)
        {
            OverrideStatus.Text = "Enter a valid Steam App ID.";
            return;
        }

        LaunchPlatform? platform = null;
        if (OverridePlatformCombo.SelectedItem is Microsoft.UI.Xaml.Controls.ComboBoxItem item &&
            item.Tag is string tag &&
            !string.IsNullOrWhiteSpace(tag) &&
            Enum.TryParse<LaunchPlatform>(tag, out var parsed))
        {
            platform = parsed;
        }

        var repo = App.Services.GetRequiredService<GameOverrideRepository>();
        await repo.SaveAsync(new GameOverride(
            appId,
            OverrideDepthSlider.Value,
            OverrideConvergenceSlider.Value,
            platform,
            false,
            "Auto"));
        OverrideStatus.Text = $"Saved override for app {appId}.";
    }

    private async void DisableHdr_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var watchdog = App.Services.GetService<HdrWatchdogService>();
        if (watchdog is null)
        {
            return;
        }

        var disabled = await watchdog.DisableHdrFor3DAsync();
        HdrNoticeBlock.Text = disabled
            ? "HDR disable requested. Confirm in Windows display settings if needed."
            : "HDR already disabled or unavailable.";
    }

    private async void BulkPreset_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var presets = App.Services.GetRequiredService<PresetCacheService>();
        var hub = App.Services.GetRequiredService<OperationProgressHub>();
        BulkPresetStatus.Text = "Caching presets…";
        await presets.BulkCacheTopPresetsAsync(50, hub);
        BulkPresetStatus.Text = "Top presets cached.";
    }

    private async void Benchmark_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var benchmark = App.Services.GetRequiredService<BenchmarkService>();
        var score = await benchmark.RunBenchmarkAsync();
        BenchmarkResult.Text = $"Benchmark score: {score:F0}";
    }

    private async void ThemeCombo_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
    {
        if (ThemeCombo.SelectedItem is not Microsoft.UI.Xaml.Controls.ComboBoxItem item || item.Tag is not string tag)
        {
            return;
        }

        var prefs = App.Services.GetRequiredService<UserPreferencesService>();
        await _settingsViewModel!.SetThemeAsync(tag);
    }

    private void LibrarySettings_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ShellPage.Current?.NavigateToTag("library-settings");
    }

    private static string BuildOpenXrOffStatusText(string? selectedRuntimeId, string? effectiveRuntime)
    {
        if (string.Equals(selectedRuntimeId, "off", StringComparison.OrdinalIgnoreCase))
        {
            return IsSteamVrInstalled()
                ? "OpenXR disabled — SteamVR and other VR runtimes are ignored for PCVR launches."
                : "OpenXR override disabled.";
        }

        return effectiveRuntime is null
            ? "No OpenXR runtime detected — PCVR launches may fail."
            : $"Effective runtime: {effectiveRuntime}";
    }

    private static bool IsSteamVrInstalled()
    {
        var steamVr = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Steam",
            "steamapps",
            "common",
            "SteamVR");
        return Directory.Exists(steamVr);
    }
}
