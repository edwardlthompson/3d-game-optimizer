namespace SpatialLabsOptimizer;

/// <summary>Setup wizard step gating shared by ViewModel and accessibility tests.</summary>
public static class SetupWizardFlow
{
    public static bool CanProceed(int currentStep, bool disclaimerAccepted) => currentStep switch
    {
        0 => disclaimerAccepted,
        _ => true
    };
}
