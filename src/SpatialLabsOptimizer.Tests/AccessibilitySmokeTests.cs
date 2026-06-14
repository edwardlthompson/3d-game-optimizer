namespace SpatialLabsOptimizer.Tests;

public class AccessibilitySmokeTests
{
    [Fact]
    public void SetupWizard_CanProceed_RequiresDisclaimerOnStepZero()
    {
        Assert.False(SetupWizardFlow.CanProceed(0, disclaimerAccepted: false));
        Assert.True(SetupWizardFlow.CanProceed(0, disclaimerAccepted: true));
    }

    [Fact]
    public void SetupWizard_CanProceed_AllowsLaterStepsWithoutDisclaimerRecheck()
    {
        Assert.True(SetupWizardFlow.CanProceed(1, disclaimerAccepted: false));
    }

    [Fact]
    public void AccessibilityIds_SetupWizardControlsAreDefined()
    {
        Assert.Equal("SetupWizard_DisclaimerCheck", AccessibilityIds.SetupWizardDisclaimer);
        Assert.Equal("SetupWizard_Continue", AccessibilityIds.SetupWizardContinue);
        Assert.Equal("SetupWizard_Benchmark", AccessibilityIds.SetupWizardBenchmark);
        Assert.False(string.IsNullOrWhiteSpace(AccessibilityIds.CommandPaletteSearch));
    }
}
