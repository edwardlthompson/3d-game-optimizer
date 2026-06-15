using SpatialLabsOptimizer.Application.Progress;
using SpatialLabsOptimizer.Infrastructure.Progress;

namespace SpatialLabsOptimizer.ViewModels;

public sealed partial class ToolchainSetupViewModel
{
    public async Task InstallToolAsync(string toolId)
    {
        if (!CanInstall)
        {
            Status = "Accept the toolchain disclaimer before installing.";
            return;
        }

        var item = RequiredTools.FirstOrDefault(t =>
            string.Equals(t.ToolId, toolId, StringComparison.OrdinalIgnoreCase));
        if (item is null || !item.CanSilentInstall)
        {
            await OpenToolGuideAsync(toolId);
            return;
        }

        IsInstallRunning = true;
        InstallLog.Clear();
        item.State = ToolInstallState.Downloading;
        try
        {
            await _installer.ExecuteAsync([toolId]);
            await ReprobeRequiredToolsAsync();
            await TryMarkSetupCompleteAsync();
            Status = $"{item.Name} install finished.";
        }
        catch (Exception ex)
        {
            item.State = ToolInstallState.Missing;
            Status = $"{item.Name} install failed: {ex.Message}";
            InstallLog.Add(ex.Message);
        }
        finally
        {
            IsInstallRunning = false;
            RefreshInstallButtonVisibility();
        }
    }

    private async Task InstallAllMissingAsync()
    {
        if (!CanInstall || SelectedDisplay is null)
        {
            return;
        }

        var ids = RequiredTools
            .Where(t => t.CanSilentInstall && t.State == ToolInstallState.Missing)
            .Select(t => t.ToolId)
            .ToList();
        if (ids.Count == 0)
        {
            Status = "No silent-install tools missing.";
            return;
        }

        IsInstallRunning = true;
        InstallLog.Clear();
        foreach (var item in RequiredTools.Where(t => ids.Contains(t.ToolId)))
        {
            item.State = ToolInstallState.Downloading;
        }

        try
        {
            await _installer.ExecuteAsync(ids);
            await ReprobeRequiredToolsAsync();
            await TryMarkSetupCompleteAsync();
            Status = "Toolchain install batch finished.";
        }
        finally
        {
            IsInstallRunning = false;
            RefreshInstallButtonVisibility();
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

        var detail = string.IsNullOrWhiteSpace(report.DetailMessage)
            ? report.CurrentStep
            : $"{report.CurrentStep} — {report.DetailMessage}";
        var line = $"{detail} ({report.PercentComplete:F0}%)";
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
    }
}
