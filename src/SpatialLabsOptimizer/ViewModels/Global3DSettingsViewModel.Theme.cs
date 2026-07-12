namespace SpatialLabsOptimizer.ViewModels;

public sealed partial class Global3DSettingsViewModel
{
    public bool SimpleMode
    {
        get => _simpleMode;
        set
        {
            if (SetProperty(ref _simpleMode, value) && !_isLoading)
            {
                OnPropertyChanged(nameof(ShowAdvancedSettings));
                OnPropertyChanged(nameof(ShowIntegrationsPanel));
                _ = SaveLaunchSafetyAsync();
            }
        }
    }

    public bool ShowAdvancedSettings => !SimpleMode;

    public bool ShowIntegrationsPanel => ShowAdvancedSettings && V2PanelVisible;

    public string Theme
    {
        get => _theme;
        private set => SetProperty(ref _theme, value);
    }

    public int ThemeIndex
    {
        get => Theme switch
        {
            "light" => 1,
            "dark" => 2,
            _ => 0
        };
        set => _ = SetThemeAsync(value switch
        {
            1 => "light",
            2 => "dark",
            _ => "system"
        });
    }

    public async Task SetThemeAsync(string theme)
    {
        Theme = theme;
        OnPropertyChanged(nameof(ThemeIndex));
        await _prefs.SetThemeAsync(theme);
        _themeService.ApplyToShell(theme);
    }
}
