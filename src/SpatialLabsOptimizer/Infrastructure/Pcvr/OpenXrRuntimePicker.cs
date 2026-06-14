using SpatialLabsOptimizer.Infrastructure.Settings;

namespace SpatialLabsOptimizer.Infrastructure.Pcvr;

public sealed record OpenXrRuntimeOption(string Id, string Label, bool IsDetected);

public sealed class OpenXrRuntimePicker
{
    internal const string PreferenceKey = "openxr_runtime_override";

    private static readonly OpenXrRuntimeOption AutoOption = new("auto", "Auto (detected)", false);

    private readonly UserPreferencesService _prefs;

    public OpenXrRuntimePicker(UserPreferencesService prefs)
    {
        _prefs = prefs;
    }

    public IReadOnlyList<OpenXrRuntimeOption> GetOptions()
    {
        var detected = OpenXrRuntimeProbe.TryResolveActiveRuntimeLabel(skipOverride: true);
        var options = new List<OpenXrRuntimeOption> { AutoOption };
        if (detected is not null)
        {
            options.Add(new OpenXrRuntimeOption("detected", detected, true));
        }

        options.Add(new OpenXrRuntimeOption("steamvr", "SteamVR / OpenXR", false));
        options.Add(new OpenXrRuntimeOption("meta-link", "Meta Quest Link", false));
        options.Add(new OpenXrRuntimeOption("virtual-desktop", "Virtual Desktop", false));
        return options;
    }

    public async Task<string> GetSelectedOverrideIdAsync(CancellationToken cancellationToken = default)
    {
        var stored = await _prefs.GetOpenXrRuntimeOverrideAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(stored) ? AutoOption.Id : stored;
    }

    public async Task SetSelectedOverrideIdAsync(string overrideId, CancellationToken cancellationToken = default)
    {
        await _prefs.SetOpenXrRuntimeOverrideAsync(
            overrideId == AutoOption.Id ? "" : overrideId,
            cancellationToken);
    }

    public async Task<string?> ResolveEffectiveRuntimeLabelAsync(CancellationToken cancellationToken = default)
    {
        var overrideId = await GetSelectedOverrideIdAsync(cancellationToken);
        return OpenXrRuntimeProbe.TryResolveActiveRuntimeLabel(overrideId);
    }
}
