using System.Collections.ObjectModel;
using System.Windows.Input;
using SpatialLabsOptimizer.Application.Progress;
using SpatialLabsOptimizer.Application.UseCases;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Install;
using SpatialLabsOptimizer.Infrastructure.Pcvr;
using SpatialLabsOptimizer.Infrastructure.Performance;
using SpatialLabsOptimizer.Infrastructure.Progress;

namespace SpatialLabsOptimizer.ViewModels;

public sealed class SetupWizardViewModel : ViewModelBase
{
    private readonly RunSilentSetup _setup;
    private readonly BenchmarkService _benchmark;
    private readonly DisplayAutoDetector _detector;
    private readonly IDisplayEdidProbe _probe;
    private readonly SqliteSettingsStore _settings;
    private readonly MuxGpuDetector _muxDetector;
    private readonly ViewingDistanceCoach _distanceCoach;
    private readonly ReadinessScoreService _readinessScore;
    private readonly OperationProgressHub _progressHub;
    private readonly ToolInstallDetector _toolDetector;
    private readonly JsonDataLoader _dataLoader;

    private int _currentStep;
    private bool _disclaimerAccepted;
    private bool _offlineOnboarding;
    private bool _isInstallRunning;
    private bool _isBenchmarkRunning;
    private int _installProgress;
    private int _readinessScoreValue;
    private string _readinessSummary = "";
    private string _status = "Welcome to 3D Game Optimizer";
    private string _benchmarkResult = "";
    private string _muxWarning = "";
    private string _viewingDistanceTip = "";

    public SetupWizardViewModel(
        RunSilentSetup setup,
        BenchmarkService benchmark,
        DisplayAutoDetector detector,
        IDisplayEdidProbe probe,
        SqliteSettingsStore settings,
        MuxGpuDetector muxDetector,
        ViewingDistanceCoach distanceCoach,
        ReadinessScoreService readinessScore,
        OperationProgressHub progressHub,
        ToolInstallDetector toolDetector,
        JsonDataLoader dataLoader)
    {
        _setup = setup;
        _benchmark = benchmark;
        _detector = detector;
        _probe = probe;
        _settings = settings;
        _muxDetector = muxDetector;
        _distanceCoach = distanceCoach;
        _readinessScore = readinessScore;
        _progressHub = progressHub;
        _toolDetector = toolDetector;
        _dataLoader = dataLoader;
        InstallLog = new ObservableCollection<string>();
        RequiredTools = new ObservableCollection<ToolInstallItemViewModel>();
        RequiredTools.CollectionChanged += (_, _) => OnPropertyChanged(nameof(RequiredTools));
        _progressHub.ProgressPublished += OnProgressPublished;
    }

    public ObservableCollection<ToolInstallItemViewModel> RequiredTools { get; }

    public ObservableCollection<string> InstallLog { get; }

    public async Task RefreshRequiredToolsAsync(Domain.DisplayProfile? profile)
    {
        RequiredTools.Clear();
        if (profile is null || profile.RequiredToolIds.Count == 0)
        {
            return;
        }

        var statuses = await _toolDetector.GetStatusesAsync(profile.RequiredToolIds);
        var manualOnly = await GetManualOnlyToolIdsAsync();
        foreach (var status in statuses)
        {
            var state = status.IsInstalled
                ? ToolInstallState.Installed
                : manualOnly.Contains(status.ToolId)
                    ? ToolInstallState.ManualRequired
                    : ToolInstallState.Missing;
            RequiredTools.Add(new ToolInstallItemViewModel(status.ToolId, status.Name, state));
        }

        OnPropertyChanged(nameof(RequiredTools));
    }

    private async Task ReprobeRequiredToolsAsync()
    {
        if (SelectedDisplay is null)
        {
            return;
        }

        var statuses = await _toolDetector.GetStatusesAsync(SelectedDisplay.RequiredToolIds);
        var manualOnly = await GetManualOnlyToolIdsAsync();
        foreach (var item in RequiredTools)
        {
            var match = statuses.FirstOrDefault(s =>
                string.Equals(s.ToolId, item.ToolId, StringComparison.OrdinalIgnoreCase));
            if (match is not null && item.State != ToolInstallState.Downloading)
            {
                item.State = match.IsInstalled
                    ? ToolInstallState.Installed
                    : manualOnly.Contains(match.ToolId)
                        ? ToolInstallState.ManualRequired
                        : ToolInstallState.Missing;
            }
        }
    }

    private async Task<HashSet<string>> GetManualOnlyToolIdsAsync()
    {
        var manifest = await _dataLoader.LoadAsync<ToolManifestDocument>("tools/tool-manifest-v1.json");
        return manifest?.Tools?
            .Where(t => t.IsManualOnly)
            .Select(t => t.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];
    }

    public string MuxWarning
    {
        get => _muxWarning;
        set => SetProperty(ref _muxWarning, value);
    }

    public string ViewingDistanceTip
    {
        get => _viewingDistanceTip;
        set => SetProperty(ref _viewingDistanceTip, value);
    }

    public int CurrentStep
    {
        get => _currentStep;
        set => SetProperty(ref _currentStep, value);
    }

    public bool DisclaimerAccepted
    {
        get => _disclaimerAccepted;
        set
        {
            if (SetProperty(ref _disclaimerAccepted, value))
            {
                ((RelayCommand)NextCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public bool OfflineOnboarding
    {
        get => _offlineOnboarding;
        set => SetProperty(ref _offlineOnboarding, value);
    }

    public bool IsInstallRunning
    {
        get => _isInstallRunning;
        private set => SetProperty(ref _isInstallRunning, value);
    }

    public bool IsBenchmarkRunning
    {
        get => _isBenchmarkRunning;
        private set => SetProperty(ref _isBenchmarkRunning, value);
    }

    public int InstallProgress
    {
        get => _installProgress;
        private set => SetProperty(ref _installProgress, value);
    }

    public int ReadinessScore
    {
        get => _readinessScoreValue;
        private set => SetProperty(ref _readinessScoreValue, value);
    }

    public string ReadinessSummary
    {
        get => _readinessSummary;
        private set => SetProperty(ref _readinessSummary, value);
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public string BenchmarkResult
    {
        get => _benchmarkResult;
        private set => SetProperty(ref _benchmarkResult, value);
    }

    public IReadOnlyList<Domain.DisplayProfile> DisplayCatalog { get; private set; } = [];

    public Domain.DisplayProfile? SelectedDisplay { get; set; }

    public string? DetectedDisplayId { get; private set; }

    public ICommand NextCommand { get; private set; } = null!;

    public bool CanProceed => SetupWizardFlow.CanProceed(CurrentStep, DisclaimerAccepted);

    public void UseOfflineOnboardingPath()
    {
        OfflineOnboarding = true;
        Status = "Offline onboarding — network downloads and preset bulk cache will be skipped";
    }

    public async Task LoadAsync()
    {
        NextCommand = new RelayCommand(async () => await NextAsync(), () => CanProceed);
        await _settings.InitializeAsync();
        DisplayCatalog = await _detector.GetCatalogAsync();
        DisclaimerAccepted = await _settings.GetDisclaimerAcceptedAsync();
        OfflineOnboarding = string.Equals(
            await _settings.GetAsync("offline_onboarding"),
            "true",
            StringComparison.OrdinalIgnoreCase);
        var mux = await _muxDetector.DetectAsync();
        MuxWarning = mux.WarningMessage ?? "";
    }

    public async Task RunBenchmarkAsync()
    {
        if (IsBenchmarkRunning)
        {
            return;
        }

        try
        {
            IsBenchmarkRunning = true;
            BenchmarkResult = "";
            Status = "Running benchmark…";
            var score = await _benchmark.RunBenchmarkAsync();
            BenchmarkResult = $"Benchmark score: {score:F0}";
            Status = "Benchmark complete.";
        }
        catch (Exception ex)
        {
            BenchmarkResult = $"Benchmark failed: {ex.Message}";
            Status = "Benchmark failed.";
        }
        finally
        {
            IsBenchmarkRunning = false;
        }
    }

    public async Task NextAsync()
    {
        switch (CurrentStep)
        {
            case 0:
                await _settings.SetDisclaimerAcceptedAsync(true);
                if (OfflineOnboarding)
                {
                    await _settings.SetAsync("offline_onboarding", "true");
                }

                CurrentStep = 1;
                Status = OfflineOnboarding
                    ? "Offline mode — detecting display without network setup…"
                    : "Detecting display…";
                SelectedDisplay ??= await _detector.DetectAsync();
                DetectedDisplayId = SelectedDisplay?.Id;
                if (SelectedDisplay is not null)
                {
                    ViewingDistanceTip = _distanceCoach.GetTipForProfile(SelectedDisplay.Id);
                    var snapshots = _probe.GetCurrentSnapshots();
                    var sig = snapshots.FirstOrDefault()?.EdidSignature ?? "unknown";
                    Status = SelectedDisplay.Id == "generic-manual"
                        ? $"Display not auto-detected (signature: {sig}). Pick your panel manually."
                        : $"Detected {SelectedDisplay.MarketingName} (signature: {sig}).";
                }
                else
                {
                    Status = "Display not detected — pick your panel manually.";
                }

                await RefreshRequiredToolsAsync(SelectedDisplay);
                break;
            case 1:
                if (OfflineOnboarding)
                {
                    CurrentStep = 2;
                    Status = "Offline setup — skipped toolchain downloads";
                    break;
                }

                InstallLog.Clear();
                IsInstallRunning = true;
                InstallProgress = 0;
                Status = "Running silent install…";
                var installOk = false;
                try
                {
                    installOk = await _setup.ExecuteAsync(SelectedDisplay);
                    Status = installOk
                        ? "Silent install complete."
                        : "Silent install failed — check logs and try again.";
                }
                catch (Exception ex)
                {
                    Status = $"Silent install failed: {ex.Message}";
                }
                finally
                {
                    IsInstallRunning = false;
                    InstallProgress = 100;
                    await ReprobeRequiredToolsAsync();
                }

                if (installOk)
                {
                    CurrentStep = 2;
                }

                break;
            case 2:
                var score = await _readinessScore.ComputeAsync(
                    SelectedDisplay,
                    OfflineOnboarding,
                    MuxWarning);
                ReadinessScore = score.Score;
                ReadinessSummary = string.Join(" · ", score.Factors);
                CurrentStep = 3;
                Status = $"Setup complete — 3D readiness {ReadinessScore}/100";
                await _settings.SetAsync("readiness_score", ReadinessScore.ToString());
                break;
        }
    }

    private void OnProgressPublished(object? sender, OperationProgressReport report)
    {
        if (report.Category != OperationCategory.Setup || !IsInstallRunning)
        {
            return;
        }

        RunOnUiThread(() => ApplyInstallProgress(report));
    }

    private void ApplyInstallProgress(OperationProgressReport report)
    {
        Status = $"{report.Title}: {report.CurrentStep}";
        if (report.PercentComplete is not null)
        {
            InstallProgress = (int)Math.Round(report.PercentComplete.Value);
        }

        var line = $"{report.CurrentStep} ({report.PercentComplete:F0}%)";
        if (InstallLog.Count == 0 || InstallLog[^1] != line)
        {
            InstallLog.Add(line);
        }

        foreach (var item in RequiredTools)
        {
            if (report.CurrentStep?.Contains(item.Name, StringComparison.OrdinalIgnoreCase) != true)
            {
                continue;
            }

            if (report.CurrentStep.Contains("Skipped", StringComparison.OrdinalIgnoreCase))
            {
                item.State = ToolInstallState.ManualRequired;
            }
            else if (report.IsFailed)
            {
                item.State = ToolInstallState.Missing;
            }
            else if (report.IsComplete)
            {
                item.State = ToolInstallState.Installed;
            }
            else
            {
                item.State = ToolInstallState.Downloading;
            }
        }

        if (report.IsComplete && string.Equals(report.OperationId, "silent-install", StringComparison.Ordinal))
        {
            _ = ReprobeRequiredToolsAsync();
        }
    }
}
