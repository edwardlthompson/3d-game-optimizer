namespace SpatialLabsOptimizer;

/// <summary>Setup wizard step gating shared by ViewModel and accessibility tests.</summary>
public static class SetupWizardFlow
{
    public const int StepDisclaimer = 0;
    public const int StepDisplay = 1;
    public const int StepToolchain = 2;
    public const int StepCount = 3;

    public static bool CanProceed(int currentStep, bool disclaimerAccepted) => currentStep switch
    {
        StepDisclaimer => disclaimerAccepted,
        _ => true
    };

    public static bool CanGoNext(int currentStep, bool disclaimerAccepted, bool displaySelected)
        => currentStep switch
        {
            StepDisclaimer => disclaimerAccepted,
            StepDisplay => displaySelected,
            StepToolchain => true,
            _ => false
        };

    public static int NextStep(int currentStep) => Math.Min(currentStep + 1, StepCount - 1);

    public static int PreviousStep(int currentStep) => Math.Max(currentStep - 1, 0);
}
