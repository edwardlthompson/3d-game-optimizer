namespace SpatialLabsOptimizer.Infrastructure.Displays;

public sealed class LaunchDisplayHandoffService
{
    private readonly MultiMonitorLaunchPicker _picker;

    public LaunchDisplayHandoffService(MultiMonitorLaunchPicker picker)
    {
        _picker = picker;
    }

    public async Task<LaunchDisplayTarget?> PrepareAsync(CancellationToken cancellationToken = default)
        => await _picker.GetSelectedTargetAsync(cancellationToken);

    public static string FormatHandoffMessage(LaunchDisplayTarget? target)
        => target is null
            ? "Using primary display."
            : $"Launch target: {target.FriendlyName}";
}
