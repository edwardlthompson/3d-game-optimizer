namespace SpatialLabsOptimizer.ViewModels;

public enum ToolInstallState
{
    Missing,
    Installed,
    Downloading,
    ManualRequired
}

public sealed class ToolInstallItemViewModel : ViewModelBase
{
    private readonly Func<bool>? _canInstall;
    private ToolInstallState _state;
    private string _detectionNote = "";

    public ToolInstallItemViewModel(
        string toolId,
        string name,
        ToolInstallState state = ToolInstallState.Missing,
        string manualInstallGuide = "",
        string vendorUrl = "",
        bool canSilentInstall = false,
        Func<bool>? canInstall = null)
    {
        ToolId = toolId;
        Name = name;
        _state = state;
        ManualInstallGuide = manualInstallGuide;
        VendorUrl = vendorUrl;
        CanSilentInstall = canSilentInstall;
        _canInstall = canInstall;
    }

    public string ToolId { get; }
    public string Name { get; }
    public string ManualInstallGuide { get; }
    public string VendorUrl { get; }
    public bool CanSilentInstall { get; }

    public bool ShowInstallButton => CanSilentInstall && State == ToolInstallState.Missing && (_canInstall?.Invoke() ?? true);

    public bool ShowActions => State is ToolInstallState.Missing or ToolInstallState.ManualRequired;

    public ToolInstallState State
    {
        get => _state;
        set
        {
            if (SetProperty(ref _state, value))
            {
                OnPropertyChanged(nameof(StatusGlyph));
                OnPropertyChanged(nameof(StatusLabel));
                OnPropertyChanged(nameof(ShowActions));
                OnPropertyChanged(nameof(ShowInstallButton));
            }
        }
    }

    public string DetectionNote
    {
        get => _detectionNote;
        set => SetProperty(ref _detectionNote, value);
    }

    public void NotifyInstallEligibilityChanged()
    {
        OnPropertyChanged(nameof(ShowInstallButton));
    }

    public string StatusGlyph => State switch
    {
        ToolInstallState.Installed => "\u2713",
        ToolInstallState.Downloading => "\u2026",
        ToolInstallState.ManualRequired => "!",
        _ => "\u2717"
    };

    public string StatusLabel => State switch
    {
        ToolInstallState.Installed => string.IsNullOrWhiteSpace(DetectionNote) ? "Installed" : $"Installed ({DetectionNote})",
        ToolInstallState.Downloading => "Installing…",
        ToolInstallState.ManualRequired => "Manual install required",
        _ => "Not installed"
    };
}
