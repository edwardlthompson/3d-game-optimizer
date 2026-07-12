using System.Windows.Input;
using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.ViewModels;

namespace SpatialLabsOptimizer.ViewModels;

public sealed class SetupWizardViewModel : ViewModelBase
{
    private readonly ToolchainSetupViewModel _toolchain;
    private readonly SqliteSettingsStore _settings;
    private int _step;
    private string _status = "Welcome — three steps to Play in 3D.";

    public SetupWizardViewModel(ToolchainSetupViewModel toolchain, SqliteSettingsStore settings)
    {
        _toolchain = toolchain;
        _settings = settings;
        NextCommand = new RelayCommand(GoNextAsync, () => CanGoNext);
        BackCommand = new RelayCommand(() => Step = SetupWizardFlow.PreviousStep(Step), () => Step > 0);
        FinishCommand = new RelayCommand(FinishAsync, () => Step == SetupWizardFlow.StepToolchain);
    }

    public ToolchainSetupViewModel Toolchain => _toolchain;

    public int Step
    {
        get => _step;
        set
        {
            if (SetProperty(ref _step, value))
            {
                OnPropertyChanged(nameof(StepTitle));
                OnPropertyChanged(nameof(IsDisclaimerStep));
                OnPropertyChanged(nameof(IsDisplayStep));
                OnPropertyChanged(nameof(IsToolchainStep));
                OnPropertyChanged(nameof(CanGoNext));
                (NextCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (BackCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (FinishCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public string StepTitle => Step switch
    {
        SetupWizardFlow.StepDisclaimer => "1 · Legal & privacy",
        SetupWizardFlow.StepDisplay => "2 · Choose your 3D display",
        _ => "3 · Install toolchain"
    };

    public bool IsDisclaimerStep => Step == SetupWizardFlow.StepDisclaimer;
    public bool IsDisplayStep => Step == SetupWizardFlow.StepDisplay;
    public bool IsToolchainStep => Step == SetupWizardFlow.StepToolchain;

    public bool DisclaimerAccepted
    {
        get => _toolchain.DisclaimerAccepted;
        set
        {
            _toolchain.DisclaimerAccepted = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanGoNext));
            (NextCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    public bool CanGoNext => SetupWizardFlow.CanGoNext(
        Step,
        DisclaimerAccepted,
        _toolchain.SelectedDisplay is not null);

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public ICommand NextCommand { get; }
    public ICommand BackCommand { get; }
    public ICommand FinishCommand { get; }

    public event Action? Finished;

    public async Task LoadAsync()
    {
        await _toolchain.LoadAsync();
        Step = SetupWizardFlow.StepDisclaimer;
        Status = "Accept the disclaimer to continue.";
    }

    public async Task OnDisplaySelectedAsync(DisplayProfile profile)
    {
        await _toolchain.OnDisplaySelectedAsync(profile);
        OnPropertyChanged(nameof(CanGoNext));
        (NextCommand as RelayCommand)?.RaiseCanExecuteChanged();
        Status = $"Display: {profile.MarketingName}";
    }

    private async Task GoNextAsync()
    {
        if (!CanGoNext)
        {
            return;
        }

        if (Step == SetupWizardFlow.StepDisclaimer)
        {
            await _settings.SetDisclaimerAcceptedAsync(true);
        }

        Step = SetupWizardFlow.NextStep(Step);
        Status = StepTitle;
    }

    private async Task FinishAsync()
    {
        await _toolchain.TryMarkSetupCompleteAsync();
        if (await _settings.GetSetupCompletedAtAsync() is null && _toolchain.SelectedDisplay is not null)
        {
            // Allow finish after display pick even if tools still installing manually.
            await _settings.SetSetupCompletedAtAsync(DateTimeOffset.UtcNow);
        }

        Status = "Setup complete — opening Library.";
        Finished?.Invoke();
    }
}
