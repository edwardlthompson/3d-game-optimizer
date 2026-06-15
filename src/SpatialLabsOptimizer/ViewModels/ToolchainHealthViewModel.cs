using System.Windows.Input;
using SpatialLabsOptimizer.Infrastructure.Launch.Coexistence;
using SpatialLabsOptimizer.Infrastructure.Settings;

namespace SpatialLabsOptimizer.ViewModels;

public sealed class ToolchainHealthViewModel : ViewModelBase
{
    private readonly UserPreferencesService _prefs;
    private readonly ExternalToolCoexistenceService _coexistence;

    private bool _safeLaunch;
    private bool _trainerCoexistence;
    private bool _modManagerCoexistence;
    private bool _simpleMode;
    private string _detectedToolsText = "Detected external tools: none";
    private bool _isLoading;

    public ToolchainHealthViewModel(
        UserPreferencesService prefs,
        ExternalToolCoexistenceService coexistence)
    {
        _prefs = prefs;
        _coexistence = coexistence;
        RefreshDetectedToolsCommand = new RelayCommand(RefreshDetectedTools);
    }

    public bool SafeLaunch
    {
        get => _safeLaunch;
        set
        {
            if (SetProperty(ref _safeLaunch, value) && !_isLoading)
            {
                _ = SavePreferencesAsync();
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
                _ = SavePreferencesAsync();
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
                _ = SavePreferencesAsync();
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
                _ = SavePreferencesAsync();
            }
        }
    }

    public string DetectedToolsText
    {
        get => _detectedToolsText;
        private set => SetProperty(ref _detectedToolsText, value);
    }

    public ICommand RefreshDetectedToolsCommand { get; }

    public async Task LoadAsync()
    {
        _isLoading = true;
        SafeLaunch = await _prefs.GetSafeLaunchAsync();
        TrainerCoexistence = await _prefs.GetTrainerCoexistenceAsync();
        ModManagerCoexistence = await _prefs.GetModManagerCoexistenceAsync();
        SimpleMode = await _prefs.GetSimpleModeAsync();
        RefreshDetectedTools();
        _isLoading = false;
    }

    public void RefreshDetectedTools()
    {
        var detected = _coexistence.GetAllRunningExternalTools();
        DetectedToolsText = detected.Count == 0
            ? "Detected external tools: none"
            : $"Detected external tools: {string.Join(", ", detected)}";
    }

    private async Task SavePreferencesAsync()
    {
        await _prefs.SetSafeLaunchAsync(SafeLaunch);
        await _prefs.SetTrainerCoexistenceAsync(TrainerCoexistence);
        await _prefs.SetModManagerCoexistenceAsync(ModManagerCoexistence);
        await _prefs.SetSimpleModeAsync(SimpleMode);
    }
}
