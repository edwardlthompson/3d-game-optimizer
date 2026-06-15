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
    public void AccessibilityIds_ToolchainControlsAreDefined()
    {
        Assert.Equal("Toolchain_DisclaimerCheck", AccessibilityIds.ToolchainDisclaimer);
        Assert.False(string.IsNullOrWhiteSpace(AccessibilityIds.CommandPaletteSearch));
    }
}
