using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Library;
using SpatialLabsOptimizer.Infrastructure.Performance;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Responsive;
using SpatialLabsOptimizer.Infrastructure.Settings;
using SpatialLabsOptimizer.Infrastructure.Updates;

namespace SpatialLabsOptimizer.ViewModels;

public sealed class ShellViewModel : ViewModelBase
{
    private readonly OperationProgressHub _progressHub;
    private readonly ResponsiveStateService _responsive;
    private readonly SystemSpecsScanner _specsScanner;
    private readonly LibraryIndexer _indexer;
    private readonly SqliteSettingsStore _settings;
    private readonly UpdateScheduler _updateScheduler;
    private readonly DisplayChangeMonitor _displayChangeMonitor;
    private readonly UserPreferencesService _prefs;

    private string _title = "3D Game Optimizer";
    private string _status = "Ready";
    private string _activityMessage = "";
    private bool _showActivityBar;
    private double _activityProgress;
    private bool _showLaunchOverlay;
    private string _launchGameTitle = "";
    private string _launchStep = "";
    private double _launchProgressPercent;
    private bool _updateAvailable;
    private bool _showDisplayChangePrompt;
    private string _displayChangeMessage = "";
    private bool _pendingSetupWizardRerun;

    public ShellViewModel(
        OperationProgressHub progressHub,
        ResponsiveStateService responsive,
        SystemSpecsScanner specsScanner,
        LibraryIndexer indexer,
        SqliteSettingsStore settings,
        UpdateScheduler updateScheduler,
        DisplayChangeMonitor displayChangeMonitor,
        UserPreferencesService prefs)
    {
        _progressHub = progressHub;
        _responsive = responsive;
        _specsScanner = specsScanner;
        _indexer = indexer;
        _settings = settings;
        _updateScheduler = updateScheduler;
        _displayChangeMonitor = displayChangeMonitor;
        _prefs = prefs;
        _progressHub.ProgressPublished += OnProgressPublished;
        _responsive.StateChanged += (_, _) => OnPropertyChanged(nameof(CurrentColumns));
        _displayChangeMonitor.ConfigurationChanged += OnDisplayConfigurationChanged;
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public string ActivityMessage
    {
        get => _activityMessage;
        set => SetProperty(ref _activityMessage, value);
    }

    public bool ShowActivityBar
    {
        get => _showActivityBar;
        set => SetProperty(ref _showActivityBar, value);
    }

    public double ActivityProgress
    {
        get => _activityProgress;
        set => SetProperty(ref _activityProgress, value);
    }

    public bool ShowLaunchOverlay
    {
        get => _showLaunchOverlay;
        set => SetProperty(ref _showLaunchOverlay, value);
    }

    public string LaunchGameTitle
    {
        get => _launchGameTitle;
        set => SetProperty(ref _launchGameTitle, value);
    }

    public string LaunchStep
    {
        get => _launchStep;
        set => SetProperty(ref _launchStep, value);
    }

    public double LaunchProgressPercent
    {
        get => _launchProgressPercent;
        set => SetProperty(ref _launchProgressPercent, value);
    }

    public bool UpdateAvailable
    {
        get => _updateAvailable;
        private set => SetProperty(ref _updateAvailable, value);
    }

    public bool ShowDisplayChangePrompt
    {
        get => _showDisplayChangePrompt;
        set => SetProperty(ref _showDisplayChangePrompt, value);
    }

    public string DisplayChangeMessage
    {
        get => _displayChangeMessage;
        set => SetProperty(ref _displayChangeMessage, value);
    }

    public bool PendingSetupWizardRerun
    {
        get => _pendingSetupWizardRerun;
        private set => SetProperty(ref _pendingSetupWizardRerun, value);
    }

    public int CurrentColumns => _responsive.CurrentColumns;

    public void StartDisplayMonitoring() => _displayChangeMonitor.Start(TimeSpan.FromSeconds(5));

    public void AcknowledgeDisplayChange() => ShowDisplayChangePrompt = false;

    public void RequestSetupWizardRerun()
    {
        ShowDisplayChangePrompt = false;
        PendingSetupWizardRerun = true;
        Status = "Display changed — re-run setup wizard to refresh EDID profile.";
    }

    public void ClearSetupWizardRerunRequest() => PendingSetupWizardRerun = false;

    public async Task InitializeAsync()
    {
        await _settings.InitializeAsync();
        Status = "Scanning hardware…";
        await _specsScanner.ScanAsync();
        Status = "Indexing library…";
        await _indexer.IndexAsync();
        await _updateScheduler.RunIfDueAsync();
        await ClearUpdateRestartPendingIfAppliedAsync();
        UpdateAvailable = _updateScheduler.IsUpdateAvailable;
        Status = "Ready";
    }

    private async Task ClearUpdateRestartPendingIfAppliedAsync()
    {
        if (!await _prefs.GetUpdateRestartPendingAsync())
        {
            return;
        }

        var applied = await _prefs.GetUpdateAppliedVersionAsync();
        var current = ProductVersionReader.ReadCurrentVersion();
        if (applied is not null && !SemverComparer.IsNewer(applied, current))
        {
            await _prefs.SetUpdateRestartPendingAsync(false);
        }
    }

    private void OnProgressPublished(object? sender, OperationProgressReport report)
        => RunOnUiThread(() => ApplyProgressReport(report));

    private void ApplyProgressReport(OperationProgressReport report)
    {
        if (report.Category == Application.Progress.OperationCategory.Launch)
        {
            ShowLaunchOverlay = !report.IsComplete && !report.IsFailed;
            LaunchGameTitle = report.Title;
            LaunchStep = report.CurrentStep;
            LaunchProgressPercent = report.PercentComplete ?? LaunchProgressPercent;
        }
        else
        {
            ShowActivityBar = !report.IsComplete && !report.IsFailed;
            ActivityMessage = $"{report.Title}: {report.CurrentStep}";
            ActivityProgress = report.PercentComplete ?? (report.IsComplete ? 100 : ActivityProgress);
        }

        if (report.IsComplete)
        {
            Status = report.CurrentStep;
        }
    }

    private void OnDisplayConfigurationChanged(object? sender, DisplayConfigurationChangedEventArgs e)
    {
        RunOnUiThread(() =>
        {
            var names = string.Join(", ", e.Current.Select(s => s.FriendlyName));
            DisplayChangeMessage =
                $"Display configuration changed ({names}). Re-run the setup wizard to refresh your EDID profile.";
            ShowDisplayChangePrompt = true;
            Status = "Display hot-plug detected";
        });
    }
}
