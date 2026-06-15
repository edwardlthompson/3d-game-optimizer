using SpatialLabsOptimizer.Infrastructure;
using SpatialLabsOptimizer.Infrastructure.Launch.Coexistence;

namespace SpatialLabsOptimizer.ViewModels;

public sealed partial class Global3DSettingsViewModel
{
    private readonly ExternalToolCoexistenceService _coexistence;

    private string _detectedToolsText = "Detected external tools: none";
    private bool _v2PanelVisible;
    private bool _v2LanEnabled;
    private bool _v2HybridEnabled;
    private bool _v2EpicGogEnabled;
    private string _v2RestartNotice = string.Empty;
    private bool _showV2RestartNotice;

    public bool V2PanelVisible
    {
        get => _v2PanelVisible;
        private set => SetProperty(ref _v2PanelVisible, value);
    }

    private bool _v2Initializing;

    public bool V2LanEnabled
    {
        get => _v2LanEnabled;
        set
        {
            if (SetProperty(ref _v2LanEnabled, value) && !_v2Initializing)
            {
                _ = ApplyV2FeaturesAsync();
            }
        }
    }

    public bool V2HybridEnabled
    {
        get => _v2HybridEnabled;
        set
        {
            if (SetProperty(ref _v2HybridEnabled, value) && !_v2Initializing)
            {
                _ = ApplyV2FeaturesAsync();
            }
        }
    }

    public bool V2EpicGogEnabled
    {
        get => _v2EpicGogEnabled;
        set
        {
            if (SetProperty(ref _v2EpicGogEnabled, value) && !_v2Initializing)
            {
                _ = ApplyV2FeaturesAsync();
            }
        }
    }

    public string V2RestartNotice
    {
        get => _v2RestartNotice;
        private set => SetProperty(ref _v2RestartNotice, value);
    }

    public bool ShowV2RestartNotice
    {
        get => _showV2RestartNotice;
        private set => SetProperty(ref _showV2RestartNotice, value);
    }

    public string DetectedToolsText
    {
        get => _detectedToolsText;
        private set => SetProperty(ref _detectedToolsText, value);
    }

    private void RefreshDetectedTools()
    {
        var detected = _coexistence.GetAllRunningExternalTools();
        DetectedToolsText = detected.Count == 0
            ? "Detected external tools: none"
            : $"Detected external tools: {string.Join(", ", detected)}";
    }

    private async Task LoadV2SectionAsync()
    {
        V2PanelVisible = true;
        _v2Initializing = true;
        var v2Enabled = await FeatureFlags.IsV2EnabledAsync(_prefs);
        V2LanEnabled = v2Enabled;
        V2HybridEnabled = v2Enabled;
        V2EpicGogEnabled = v2Enabled;
        _v2Initializing = false;
        UpdateV2RestartNotice(v2Enabled);
    }

    private async Task ApplyV2FeaturesAsync()
    {
        var wantsV2 = V2LanEnabled || V2HybridEnabled || V2EpicGogEnabled;
        await _prefs.SetV2ExperimentalAsync(wantsV2);
        UpdateV2RestartNotice(wantsV2);
    }

    private void UpdateV2RestartNotice(bool wantsV2)
    {
        if (FeatureFlags.V2EnabledFromEnvironment)
        {
            ShowV2RestartNotice = false;
            V2RestartNotice = string.Empty;
            return;
        }

        var mismatch = wantsV2 != FeatureFlags.V2RegisteredAtStartup;
        ShowV2RestartNotice = mismatch;
        V2RestartNotice = mismatch
            ? "Restart the app to apply integration changes."
            : string.Empty;
    }
}
