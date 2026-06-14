using Microsoft.Extensions.DependencyInjection;
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
        SimpleModeToggle.IsOn = await prefs.GetSimpleModeAsync();
    }

    private async void Preference_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var prefs = App.Services.GetRequiredService<UserPreferencesService>();
        await prefs.SetSafeLaunchAsync(SafeLaunchToggle.IsOn);
        await prefs.SetTrainerCoexistenceAsync(TrainerToggle.IsOn);
        await prefs.SetSimpleModeAsync(SimpleModeToggle.IsOn);
    }
}
