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
}
