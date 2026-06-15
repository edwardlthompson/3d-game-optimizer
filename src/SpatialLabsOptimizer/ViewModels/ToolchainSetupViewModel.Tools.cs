using SpatialLabsOptimizer.Infrastructure.Install;
using Windows.System;

namespace SpatialLabsOptimizer.ViewModels;

public sealed partial class ToolchainSetupViewModel
{
    public async Task RefreshRequiredToolsAsync(Domain.DisplayProfile? profile)
    {
        RequiredTools.Clear();
        if (profile is null || profile.RequiredToolIds.Count == 0)
        {
            OnPropertyChanged(nameof(RequiredTools));
            return;
        }

        var manifest = await _dataLoader.LoadAsync<ToolManifestDocument>("tools/tool-manifest-v1.json");
        var manifestById = manifest?.Tools?.ToDictionary(t => t.Id, StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, ToolManifestEntryDto>(StringComparer.OrdinalIgnoreCase);
        var manualOnly = manifest?.Tools?.Where(t => t.IsManualOnly).Select(t => t.Id).ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];
        var statuses = await _toolDetector.GetStatusesAsync(profile.RequiredToolIds);
        foreach (var status in statuses)
        {
            manifestById.TryGetValue(status.ToolId, out var entry);
            var canSilent = entry is not null && !entry.IsManualOnly &&
                (!string.IsNullOrWhiteSpace(entry.BundledPackage) || !string.IsNullOrWhiteSpace(entry.DownloadUrl));
            var state = status.IsInstalled
                ? ToolInstallState.Installed
                : manualOnly.Contains(status.ToolId)
                    ? ToolInstallState.ManualRequired
                    : ToolInstallState.Missing;
            var item = new ToolInstallItemViewModel(
                status.ToolId,
                status.Name,
                state,
                entry?.ManualInstallGuide ?? "",
                entry?.VendorUrl ?? "",
                canSilent,
                () => CanInstall);
            var note = await _toolDetector.GetDetectionNoteAsync(status.ToolId);
            if (!string.IsNullOrWhiteSpace(note))
            {
                item.DetectionNote = note;
            }

            RequiredTools.Add(item);
        }

        OnPropertyChanged(nameof(RequiredTools));
    }

    public async Task OpenToolGuideAsync(string toolId)
    {
        var manifest = await _dataLoader.LoadAsync<ToolManifestDocument>("tools/tool-manifest-v1.json");
        var tool = manifest?.Tools?.FirstOrDefault(t =>
            string.Equals(t.Id, toolId, StringComparison.OrdinalIgnoreCase));
        if (tool is null)
        {
            Status = $"No install guide for {toolId}.";
            return;
        }

        if (!string.IsNullOrWhiteSpace(tool.VendorUrl))
        {
            await Launcher.LaunchUriAsync(new Uri(tool.VendorUrl));
            Status = $"Opened vendor page for {tool.Name}.";
            return;
        }

        Status = string.IsNullOrWhiteSpace(tool.ManualInstallGuide)
            ? $"See docs/TOOLCHAIN.md for {tool.Name}."
            : tool.ManualInstallGuide;
    }

    public async Task RegisterToolInstallPathAsync(string toolId, string path)
    {
        await _settings.SetToolInstallPathAsync(toolId, path);
        Status = $"Registered install folder for {toolId}.";
        await ReprobeRequiredToolsAsync();
        await TryMarkSetupCompleteAsync();
    }

    public async Task VerifyToolInstallAsync(string toolId)
    {
        var installed = await _toolDetector.IsInstalledAsync(toolId);
        await ReprobeRequiredToolsAsync();
        Status = installed
            ? $"{toolId} detected."
            : $"{toolId} not detected — try Locate on disk or install from vendor.";
        await TryMarkSetupCompleteAsync();
    }

    private async Task ReprobeRequiredToolsAsync()
    {
        if (SelectedDisplay is null)
        {
            return;
        }

        var statuses = await _toolDetector.GetStatusesAsync(SelectedDisplay.RequiredToolIds);
        var manifest = await _dataLoader.LoadAsync<ToolManifestDocument>("tools/tool-manifest-v1.json");
        var manualOnly = manifest?.Tools?.Where(t => t.IsManualOnly).Select(t => t.Id).ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];
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
                var note = await _toolDetector.GetDetectionNoteAsync(match.ToolId);
                item.DetectionNote = note ?? "";
            }
        }
    }
}
