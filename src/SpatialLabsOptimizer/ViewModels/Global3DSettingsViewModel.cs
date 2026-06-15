using System.Windows.Input;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Launch.Coexistence;
using SpatialLabsOptimizer.Infrastructure.Pcvr;
using SpatialLabsOptimizer.Infrastructure.Performance;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Settings;
using SpatialLabsOptimizer.Infrastructure.Updates;

namespace SpatialLabsOptimizer.ViewModels;

public sealed partial class Global3DSettingsViewModel : ViewModelBase
{
    private readonly UserPreferencesService _prefs;

    private double _depth = 0.65;
    private double _convergence = 0.5;
    private bool _safeLaunch;
    private bool _trainerCoexistence;
    private bool _modManagerCoexistence;
    private bool _simpleMode;
    private string _theme = "system";
    private bool _expandToolchain;

    public Global3DSettingsViewModel(
        UserPreferencesService prefs,
        ExternalToolCoexistenceService coexistence,
        GameOverrideRepository overrides,
        PresetCacheService presets,
        OperationProgressHub progressHub,
        BenchmarkService benchmark,
        MultiMonitorLaunchPicker launchPicker,
        OpenXrRuntimePicker openXrPicker,
        ConfigSnapshotService snapshots,
        ViewingDistanceCoach viewingDistanceCoach,
        ToolchainSetupViewModel toolchain,
        HdrWatchdogService? hdrWatchdog = null,
        SessionProfileService? sessionProfiles = null,
        StreamerHotkeyService? streamerHotkey = null,
        StreamFriendlyProfileService? streamFriendly = null)
    {
        _prefs = prefs;
        Toolchain = toolchain;
        _coexistence = coexistence;
        _overrides = overrides;
        _presets = presets;
        _progressHub = progressHub;
        _benchmark = benchmark;
        _launchPicker = launchPicker;
        _openXrPicker = openXrPicker;
        _snapshots = snapshots;
        ViewingDistanceCoach = viewingDistanceCoach;
        _hdrWatchdog = hdrWatchdog;
        _sessionProfiles = sessionProfiles;
        _streamerHotkey = streamerHotkey;
        _streamFriendly = streamFriendly;

        RefreshDetectedToolsCommand = new RelayCommand(RefreshDetectedTools);
        SaveOverrideCommand = new RelayCommand(SaveOverrideAsync);
        DisableHdrCommand = new RelayCommand(DisableHdrAsync);
        CachePresetsCommand = new RelayCommand(CachePresetsAsync);
        RunBenchmarkCommand = new RelayCommand(RunBenchmarkAsync);
        RefreshSnapshotsCommand = new RelayCommand(RefreshSnapshots);
        RestoreSnapshotCommand = new RelayCommand(RestoreSnapshotAsync, () => CanRestoreSnapshot);
        SaveSessionProfileCommand = new RelayCommand(SaveSessionProfileAsync);
        LoadSessionProfileCommand = new RelayCommand(LoadSessionProfileAsync);
        ToggleViewingDistanceCoachCommand = new RelayCommand(ToggleViewingDistanceCoach);
    }

    public ViewingDistanceCoach ViewingDistanceCoach { get; }

    public ToolchainSetupViewModel Toolchain { get; }

    public bool ExpandToolchain
    {
        get => _expandToolchain;
        set => SetProperty(ref _expandToolchain, value);
    }

    public double Depth
    {
        get => _depth;
        set
        {
            if (SetProperty(ref _depth, value) && !_isLoading)
            {
                _ = SaveStereoscopyDefaultsAsync();
            }
        }
    }

    public double Convergence
    {
        get => _convergence;
        set
        {
            if (SetProperty(ref _convergence, value) && !_isLoading)
            {
                _ = SaveStereoscopyDefaultsAsync();
            }
        }
    }

    public bool SafeLaunch
    {
        get => _safeLaunch;
        set
        {
            if (SetProperty(ref _safeLaunch, value) && !_isLoading)
            {
                _ = SaveLaunchSafetyAsync();
            }
        }
    }

    public bool TrainerCoexistence
    {
        get => _trainerCoexistence;
        set
        {
            if (SetProperty(ref _trainerCoexistence, value) && !_isLoading)
            {
                _ = SaveLaunchSafetyAsync();
            }
        }
    }

    public bool ModManagerCoexistence
    {
        get => _modManagerCoexistence;
        set
        {
            if (SetProperty(ref _modManagerCoexistence, value) && !_isLoading)
            {
                _ = SaveLaunchSafetyAsync();
            }
        }
    }

    public bool SimpleMode
    {
        get => _simpleMode;
        set
        {
            if (SetProperty(ref _simpleMode, value) && !_isLoading)
            {
                _ = SaveLaunchSafetyAsync();
            }
        }
    }

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

    public ICommand RefreshDetectedToolsCommand { get; }
    public ICommand SaveOverrideCommand { get; }
    public ICommand DisableHdrCommand { get; }
    public ICommand CachePresetsCommand { get; }
    public ICommand RunBenchmarkCommand { get; }
    public ICommand RefreshSnapshotsCommand { get; }
    public ICommand RestoreSnapshotCommand { get; }
    public ICommand SaveSessionProfileCommand { get; }
    public ICommand LoadSessionProfileCommand { get; }
    public ICommand ToggleViewingDistanceCoachCommand { get; }

    private bool _isLoading;

    public async Task LoadAsync()
    {
        _isLoading = true;
        SafeLaunch = await _prefs.GetSafeLaunchAsync();
        TrainerCoexistence = await _prefs.GetTrainerCoexistenceAsync();
        ModManagerCoexistence = await _prefs.GetModManagerCoexistenceAsync();
        SimpleMode = await _prefs.GetSimpleModeAsync();
        Theme = await _prefs.GetThemeAsync();
        Depth = await _prefs.GetDefaultDepthAsync();
        Convergence = await _prefs.GetDefaultConvergenceAsync();
        OnPropertyChanged(nameof(ThemeIndex));

        RefreshDetectedTools();
        await LoadV2SectionAsync();
        await LoadHdrSectionAsync();
        await LoadDisplayLaunchAsync();
        await LoadSessionProfilesSectionAsync();
        RefreshSnapshots();
        await Toolchain.LoadAsync();
        _isLoading = false;
    }

    public async Task SaveLaunchSafetyAsync()
    {
        await _prefs.SetSafeLaunchAsync(SafeLaunch);
        await _prefs.SetTrainerCoexistenceAsync(TrainerCoexistence);
        await _prefs.SetModManagerCoexistenceAsync(ModManagerCoexistence);
        await _prefs.SetSimpleModeAsync(SimpleMode);
    }

    private async Task SaveStereoscopyDefaultsAsync()
    {
        await _prefs.SetDefaultDepthAsync(Depth);
        await _prefs.SetDefaultConvergenceAsync(Convergence);
    }

    public async Task SetThemeAsync(string theme)
    {
        Theme = theme;
        OnPropertyChanged(nameof(ThemeIndex));
        await _prefs.SetThemeAsync(theme);
    }
}
