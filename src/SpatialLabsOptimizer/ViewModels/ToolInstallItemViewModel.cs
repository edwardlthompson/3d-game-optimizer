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
    private ToolInstallState _state;

    public ToolInstallItemViewModel(string toolId, string name, ToolInstallState state = ToolInstallState.Missing)
    {
        ToolId = toolId;
        Name = name;
        _state = state;
    }

    public string ToolId { get; }
    public string Name { get; }

    public ToolInstallState State
    {
        get => _state;
        set
        {
            if (SetProperty(ref _state, value))
            {
                OnPropertyChanged(nameof(StatusGlyph));
                OnPropertyChanged(nameof(StatusLabel));
            }
        }
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
        ToolInstallState.Installed => "Installed",
        ToolInstallState.Downloading => "Installing…",
        ToolInstallState.ManualRequired => "Manual install required",
        _ => "Not installed"
    };
}
