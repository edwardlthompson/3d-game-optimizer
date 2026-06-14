using Microsoft.Extensions.DependencyInjection;
using SpatialLabsOptimizer.Infrastructure.Launch.Coexistence;
using SpatialLabsOptimizer.Infrastructure.Settings;

namespace SpatialLabsOptimizer.Views;

public sealed partial class ToolchainHealthView : Microsoft.UI.Xaml.Controls.Page
{
    public ToolchainHealthView()
    {
        InitializeComponent();
        Loaded += ToolchainHealthView_Loaded;
    }

    private async void ToolchainHealthView_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var prefs = App.Services.GetRequiredService<UserPreferencesService>();
        SafeLaunchToggle.IsOn = await prefs.GetSafeLaunchAsync();
        TrainerToggle.IsOn = await prefs.GetTrainerCoexistenceAsync();
        ModManagerToggle.IsOn = await prefs.GetModManagerCoexistenceAsync();
        SimpleModeToggle.IsOn = await prefs.GetSimpleModeAsync();
        RefreshDetectedTools();
    }

    private async void Preference_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var prefs = App.Services.GetRequiredService<UserPreferencesService>();
        await prefs.SetSafeLaunchAsync(SafeLaunchToggle.IsOn);
        await prefs.SetTrainerCoexistenceAsync(TrainerToggle.IsOn);
        await prefs.SetModManagerCoexistenceAsync(ModManagerToggle.IsOn);
        await prefs.SetSimpleModeAsync(SimpleModeToggle.IsOn);
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
}
