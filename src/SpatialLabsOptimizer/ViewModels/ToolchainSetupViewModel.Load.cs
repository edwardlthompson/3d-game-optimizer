namespace SpatialLabsOptimizer.ViewModels;

public sealed partial class ToolchainSetupViewModel
{
    public async Task LoadAsync()
    {
        InitializeCommands();
        DisplayCatalog = await _detector.GetCatalogAsync();
        var savedProfileId = await _settings.GetActiveDisplayProfileIdAsync();
        var detected = await _detector.DetectAsync();
        DetectedDisplayId = detected?.Id ?? savedProfileId;
        SelectedDisplay = DisplayCatalog.FirstOrDefault(p => p.Id == DetectedDisplayId)
            ?? DisplayCatalog.FirstOrDefault(p => p.Id == savedProfileId);
        if (SelectedDisplay is not null)
        {
            ViewingDistanceTip = _distanceCoach.GetTipForProfile(SelectedDisplay.Id);
            await RefreshRequiredToolsAsync(SelectedDisplay);
        }

        var completed = await _settings.GetSetupCompletedAtAsync();
        Status = completed.HasValue
            ? $"Toolchain configured — last updated {completed.Value.ToLocalTime():g}"
            : "Pick your 3D display and install required tools.";
    }

    public async Task OnDisplaySelectedAsync(Domain.DisplayProfile profile)
    {
        SelectedDisplay = profile;
        ViewingDistanceTip = _distanceCoach.GetTipForProfile(profile.Id);
        await RefreshRequiredToolsAsync(profile);
        await _settings.SetActiveDisplayProfileIdAsync(profile.Id);
    }

    public async Task TryMarkSetupCompleteAsync()
    {
        if (SelectedDisplay is null)
        {
            return;
        }

        var missing = RequiredTools.Any(t => t.State is ToolInstallState.Missing or ToolInstallState.ManualRequired);
        if (!missing)
        {
            await _settings.SetSetupCompletedAtAsync(DateTimeOffset.UtcNow);
        }
    }
}
