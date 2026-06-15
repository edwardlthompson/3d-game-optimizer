using System.Collections.ObjectModel;
using System.Windows.Input;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Install;
using SpatialLabsOptimizer.Infrastructure.Progress;

namespace SpatialLabsOptimizer.ViewModels;

public sealed partial class ToolchainSetupViewModel : ViewModelBase
{
    private readonly DisplayAutoDetector _detector;
    private readonly ToolInstallDetector _toolDetector;
    private readonly SilentInstallOrchestrator _installer;
    private readonly SqliteSettingsStore _settings;
    private readonly JsonDataLoader _dataLoader;
    private readonly OperationProgressHub _progressHub;
    private readonly ViewingDistanceCoach _distanceCoach;

    private bool _disclaimerAccepted;
    private bool _isInstallRunning;
    private int _installProgress;
    private string _status = "Pick your 3D display to see required tools.";
    private string _viewingDistanceTip = "";
    private string _spatialLabsNote =
        "SpatialLabs Experience Center is Acer's user-facing app. SpatialLabs Runtime Platform is the underlying driver/runtime this app probes via registry. Either may satisfy the requirement on SpatialLabs hardware.";

    public ToolchainSetupViewModel(
        DisplayAutoDetector detector,
        ToolInstallDetector toolDetector,
        SilentInstallOrchestrator installer,
        SqliteSettingsStore settings,
        JsonDataLoader dataLoader,
        OperationProgressHub progressHub,
        ViewingDistanceCoach distanceCoach)
    {
        _detector = detector;
        _toolDetector = toolDetector;
        _installer = installer;
        _settings = settings;
        _dataLoader = dataLoader;
        _progressHub = progressHub;
        _distanceCoach = distanceCoach;
        RequiredTools = new ObservableCollection<ToolInstallItemViewModel>();
        InstallLog = new ObservableCollection<string>();
        _progressHub.ProgressPublished += OnProgressPublished;
    }

    public ObservableCollection<ToolInstallItemViewModel> RequiredTools { get; }

    public ObservableCollection<string> InstallLog { get; }

    public bool DisclaimerAccepted
    {
        get => _disclaimerAccepted;
        set
        {
            if (SetProperty(ref _disclaimerAccepted, value))
            {
                OnPropertyChanged(nameof(CanInstall));
                RefreshInstallButtonVisibility();
            }
        }
    }

    public bool CanInstall => DisclaimerAccepted && !IsInstallRunning;

    public bool IsInstallRunning
    {
        get => _isInstallRunning;
        private set
        {
            if (SetProperty(ref _isInstallRunning, value))
            {
                OnPropertyChanged(nameof(CanInstall));
            }
        }
    }

    public int InstallProgress
    {
        get => _installProgress;
        private set => SetProperty(ref _installProgress, value);
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public string ViewingDistanceTip
    {
        get => _viewingDistanceTip;
        set => SetProperty(ref _viewingDistanceTip, value);
    }

    public string SpatialLabsNote
    {
        get => _spatialLabsNote;
        set => SetProperty(ref _spatialLabsNote, value);
    }

    public IReadOnlyList<Domain.DisplayProfile> DisplayCatalog { get; private set; } = [];

    public Domain.DisplayProfile? SelectedDisplay { get; set; }

    public string? DetectedDisplayId { get; private set; }

    public ViewingDistanceCoach ViewingDistanceCoach => _distanceCoach;

    public ICommand InstallAllMissingCommand { get; private set; } = null!;

    public void InitializeCommands()
    {
        InstallAllMissingCommand = new RelayCommand(
            async () => await InstallAllMissingAsync(),
            () => CanInstall);
    }

    private void RefreshInstallButtonVisibility()
    {
        foreach (var item in RequiredTools)
        {
            item.NotifyInstallEligibilityChanged();
        }
    }
}
