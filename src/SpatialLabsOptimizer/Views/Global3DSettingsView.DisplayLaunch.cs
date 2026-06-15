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

public sealed partial class Global3DSettingsView
{
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
