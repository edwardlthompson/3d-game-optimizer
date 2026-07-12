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
    public void SetupWizard_CanGoNext_RequiresDisplayOnStepOne()
    {
        Assert.False(SetupWizardFlow.CanGoNext(1, disclaimerAccepted: true, displaySelected: false));
        Assert.True(SetupWizardFlow.CanGoNext(1, disclaimerAccepted: true, displaySelected: true));
    }

    [Fact]
    public void SetupWizard_NextPrevious_Clamp()
    {
        Assert.Equal(1, SetupWizardFlow.NextStep(0));
        Assert.Equal(2, SetupWizardFlow.NextStep(2));
        Assert.Equal(0, SetupWizardFlow.PreviousStep(0));
    }

    [Fact]
    public void AccessibilityIds_ToolchainControlsAreDefined()
    {
        Assert.Equal("Toolchain_DisclaimerCheck", AccessibilityIds.ToolchainDisclaimer);
        Assert.False(string.IsNullOrWhiteSpace(AccessibilityIds.CommandPaletteSearch));
    }
}
