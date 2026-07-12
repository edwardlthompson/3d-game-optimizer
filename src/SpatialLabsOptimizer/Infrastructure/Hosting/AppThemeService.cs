using Microsoft.UI.Xaml;
using SpatialLabsOptimizer.Infrastructure.Settings;
using SpatialLabsOptimizer.Views;

namespace SpatialLabsOptimizer.Infrastructure.Hosting;

public sealed class AppThemeService
{
    private readonly UserPreferencesService _prefs;

    public AppThemeService(UserPreferencesService prefs) => _prefs = prefs;

    public async Task ApplySavedThemeAsync(CancellationToken cancellationToken = default)
    {
        var theme = await _prefs.GetThemeAsync(cancellationToken);
        ApplyToShell(theme);
    }

    public void ApplyToShell(string theme)
    {
        var root = ShellPage.Current;
        if (root is null)
        {
            return;
        }

        root.RequestedTheme = theme switch
        {
            "light" => ElementTheme.Light,
            "dark" => ElementTheme.Dark,
            _ => ElementTheme.Default
        };
    }
}
