using System.Windows.Input;
using SpatialLabsOptimizer.Infrastructure.Settings;

namespace SpatialLabsOptimizer.ViewModels;

public sealed class Global3DSettingsViewModel : ViewModelBase
{
    private readonly UserPreferencesService _prefs;

    private bool _safeLaunch;
    private bool _trainerCoexistence;
    private bool _modManagerCoexistence;
    private bool _simpleMode;
    private string _theme = "system";

    public Global3DSettingsViewModel(UserPreferencesService prefs)
    {
        _prefs = prefs;
        SaveLaunchSafetyCommand = new RelayCommand(SaveLaunchSafetyAsync);
    }

    public bool SafeLaunch
    {
        get => _safeLaunch;
        set => SetProperty(ref _safeLaunch, value);
    }

    public bool TrainerCoexistence
    {
        get => _trainerCoexistence;
        set => SetProperty(ref _trainerCoexistence, value);
    }

    public bool ModManagerCoexistence
    {
        get => _modManagerCoexistence;
        set => SetProperty(ref _modManagerCoexistence, value);
    }

    public bool SimpleMode
    {
        get => _simpleMode;
        set => SetProperty(ref _simpleMode, value);
    }

    public string Theme
    {
        get => _theme;
        set => SetProperty(ref _theme, value);
    }

    public ICommand SaveLaunchSafetyCommand { get; }

    public async Task LoadAsync()
    {
        SafeLaunch = await _prefs.GetSafeLaunchAsync();
        TrainerCoexistence = await _prefs.GetTrainerCoexistenceAsync();
        ModManagerCoexistence = await _prefs.GetModManagerCoexistenceAsync();
        SimpleMode = await _prefs.GetSimpleModeAsync();
        Theme = await _prefs.GetThemeAsync();
    }

    public async Task SaveLaunchSafetyAsync()
    {
        await _prefs.SetSafeLaunchAsync(SafeLaunch);
        await _prefs.SetTrainerCoexistenceAsync(TrainerCoexistence);
        await _prefs.SetModManagerCoexistenceAsync(ModManagerCoexistence);
        await _prefs.SetSimpleModeAsync(SimpleMode);
    }

    public async Task SetThemeAsync(string theme)
    {
        Theme = theme;
        await _prefs.SetThemeAsync(theme);
    }
}
