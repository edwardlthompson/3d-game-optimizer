using SpatialLabsOptimizer.Infrastructure.Settings;

namespace SpatialLabsOptimizer.Infrastructure.Displays;

public sealed record LaunchDisplayTarget(string DeviceId, string FriendlyName, bool IsPrimary);

public sealed class MultiMonitorLaunchPicker
{
    internal const string PreferenceKey = "launch_target_display";

    private readonly IDisplayEdidProbe _probe;
    private readonly UserPreferencesService _prefs;

    public MultiMonitorLaunchPicker(IDisplayEdidProbe probe, UserPreferencesService prefs)
    {
        _probe = probe;
        _prefs = prefs;
    }

    public IReadOnlyList<LaunchDisplayTarget> GetAvailableTargets()
    {
        var snapshots = _probe.GetCurrentSnapshots();
        if (snapshots.Count == 0)
        {
            return [new LaunchDisplayTarget("primary", "Primary display", true)];
        }

        return snapshots
            .Select((s, index) => new LaunchDisplayTarget(s.DeviceId, s.FriendlyName, index == 0))
            .ToList();
    }

    public async Task<LaunchDisplayTarget?> GetSelectedTargetAsync(CancellationToken cancellationToken = default)
    {
        var storedId = await _prefs.GetLaunchTargetDisplayAsync(cancellationToken);
        var targets = GetAvailableTargets();
        if (string.IsNullOrWhiteSpace(storedId))
        {
            return targets.FirstOrDefault(t => t.IsPrimary) ?? targets.FirstOrDefault();
        }

        return targets.FirstOrDefault(t => t.DeviceId == storedId)
            ?? targets.FirstOrDefault(t => t.IsPrimary)
            ?? targets.FirstOrDefault();
    }

    public async Task SetSelectedTargetAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        await _prefs.SetLaunchTargetDisplayAsync(deviceId, cancellationToken);
    }
}
