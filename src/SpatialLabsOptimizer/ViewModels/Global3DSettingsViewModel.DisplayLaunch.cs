using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Pcvr;

namespace SpatialLabsOptimizer.ViewModels;

public sealed partial class Global3DSettingsViewModel
{
    private readonly MultiMonitorLaunchPicker _launchPicker;
    private readonly OpenXrRuntimePicker _openXrPicker;

    private bool _displayLaunchInitialized;
    private IReadOnlyList<LaunchDisplayTarget> _launchDisplayTargets = [];
    private LaunchDisplayTarget? _selectedLaunchDisplay;
    private IReadOnlyList<OpenXrRuntimeOption> _openXrRuntimeOptions = [];
    private OpenXrRuntimeOption? _selectedOpenXrRuntime;
    private string _launchDisplayStatus = string.Empty;
    private string _openXrRuntimeStatus = string.Empty;

    public IReadOnlyList<LaunchDisplayTarget> LaunchDisplayTargets
    {
        get => _launchDisplayTargets;
        private set => SetProperty(ref _launchDisplayTargets, value);
    }

    public LaunchDisplayTarget? SelectedLaunchDisplay
    {
        get => _selectedLaunchDisplay;
        set
        {
            if (!SetProperty(ref _selectedLaunchDisplay, value))
            {
                return;
            }

            if (_displayLaunchInitialized && value is not null)
            {
                _ = ApplyLaunchDisplayAsync(value);
            }
        }
    }

    public IReadOnlyList<OpenXrRuntimeOption> OpenXrRuntimeOptions
    {
        get => _openXrRuntimeOptions;
        private set => SetProperty(ref _openXrRuntimeOptions, value);
    }

    public OpenXrRuntimeOption? SelectedOpenXrRuntime
    {
        get => _selectedOpenXrRuntime;
        set
        {
            if (!SetProperty(ref _selectedOpenXrRuntime, value))
            {
                return;
            }

            if (_displayLaunchInitialized && value is not null)
            {
                _ = ApplyOpenXrRuntimeAsync(value);
            }
        }
    }

    public string LaunchDisplayStatus
    {
        get => _launchDisplayStatus;
        private set => SetProperty(ref _launchDisplayStatus, value);
    }

    public string OpenXrRuntimeStatus
    {
        get => _openXrRuntimeStatus;
        private set => SetProperty(ref _openXrRuntimeStatus, value);
    }

    private async Task LoadDisplayLaunchAsync()
    {
        _displayLaunchInitialized = false;
        var selectedDisplay = await _launchPicker.GetSelectedTargetAsync();
        var selectedRuntime = await _openXrPicker.GetSelectedOverrideIdAsync();

        LaunchDisplayTargets = _launchPicker.GetAvailableTargets();
        SelectedLaunchDisplay = selectedDisplay is null
            ? null
            : LaunchDisplayTargets.FirstOrDefault(t => t.DeviceId == selectedDisplay.DeviceId);

        OpenXrRuntimeOptions = _openXrPicker.GetOptions();
        SelectedOpenXrRuntime = OpenXrRuntimeOptions.FirstOrDefault(o => o.Id == selectedRuntime);

        var effectiveRuntime = await _openXrPicker.ResolveEffectiveRuntimeLabelAsync();
        OpenXrRuntimeStatus = BuildOpenXrOffStatusText(selectedRuntime, effectiveRuntime);
        LaunchDisplayStatus = selectedDisplay is null
            ? "Using primary display."
            : $"Games launch on: {selectedDisplay.FriendlyName}";

        _displayLaunchInitialized = true;
    }

    private async Task ApplyLaunchDisplayAsync(LaunchDisplayTarget target)
    {
        await _launchPicker.SetSelectedTargetAsync(target.DeviceId);
        LaunchDisplayStatus = $"Games launch on: {target.FriendlyName}";
    }

    private async Task ApplyOpenXrRuntimeAsync(OpenXrRuntimeOption option)
    {
        await _openXrPicker.SetSelectedOverrideIdAsync(option.Id);
        var effective = await _openXrPicker.ResolveEffectiveRuntimeLabelAsync();
        OpenXrRuntimeStatus = BuildOpenXrOffStatusText(option.Id, effective);
    }

    private static string BuildOpenXrOffStatusText(string? selectedRuntimeId, string? effectiveRuntime)
    {
        if (string.Equals(selectedRuntimeId, "off", StringComparison.OrdinalIgnoreCase))
        {
            return IsSteamVrInstalled()
                ? "OpenXR disabled — SteamVR and other VR runtimes are ignored for PCVR launches."
                : "OpenXR override disabled.";
        }

        return effectiveRuntime is null
            ? "No OpenXR runtime detected — PCVR launches may fail."
            : $"Effective runtime: {effectiveRuntime}";
    }

    private static bool IsSteamVrInstalled()
    {
        var steamVr = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Steam",
            "steamapps",
            "common",
            "SteamVR");
        return Directory.Exists(steamVr);
    }
}
