using System.Windows.Input;
using SpatialLabsOptimizer.Application.UseCases;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Pcvr;
using SpatialLabsOptimizer.Infrastructure.Performance;

namespace SpatialLabsOptimizer.ViewModels;

public sealed class SetupWizardViewModel : ViewModelBase
{
    private readonly RunSilentSetup _setup;
    private readonly BenchmarkService _benchmark;
    private readonly DisplayAutoDetector _detector;
    private readonly SqliteSettingsStore _settings;
    private readonly MuxGpuDetector _muxDetector;
    private readonly ViewingDistanceCoach _distanceCoach;
    private readonly ReadinessScoreService _readinessScore;

    private int _currentStep;
    private bool _disclaimerAccepted;
    private bool _offlineOnboarding;
    private int _readinessScoreValue;
    private string _readinessSummary = "";
    private string _status = "Welcome to 3D Game Optimizer";
    private string _muxWarning = "";
    private string _viewingDistanceTip = "";

    public SetupWizardViewModel(
        RunSilentSetup setup,
        BenchmarkService benchmark,
        DisplayAutoDetector detector,
        SqliteSettingsStore settings,
        MuxGpuDetector muxDetector,
        ViewingDistanceCoach distanceCoach,
        ReadinessScoreService readinessScore)
    {
        _setup = setup;
        _benchmark = benchmark;
        _detector = detector;
        _settings = settings;
        _muxDetector = muxDetector;
        _distanceCoach = distanceCoach;
        _readinessScore = readinessScore;
        NextCommand = new RelayCommand(async () => await NextAsync(), () => CanProceed);
        RunBenchmarkCommand = new RelayCommand(async () => await _benchmark.RunBenchmarkAsync());
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

    public IReadOnlyList<Domain.DisplayProfile> DisplayCatalog { get; private set; } = [];

    public Domain.DisplayProfile? SelectedDisplay { get; set; }

    public ICommand NextCommand { get; }
    public ICommand RunBenchmarkCommand { get; }

    public bool CanProceed => SetupWizardFlow.CanProceed(CurrentStep, DisclaimerAccepted);

    public void UseOfflineOnboardingPath()
    {
        OfflineOnboarding = true;
        Status = "Offline onboarding — network downloads and preset bulk cache will be skipped";
    }

    public async Task LoadAsync()
    {
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

    private async Task NextAsync()
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
                if (SelectedDisplay is not null)
                {
                    ViewingDistanceTip = _distanceCoach.GetTipForProfile(SelectedDisplay.Id);
                }
                break;
            case 1:
                CurrentStep = 2;
                if (OfflineOnboarding)
                {
                    Status = "Offline setup — skipped toolchain downloads";
                }
                else
                {
                    Status = "Running silent install…";
                    await _setup.ExecuteAsync(SelectedDisplay);
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
}
