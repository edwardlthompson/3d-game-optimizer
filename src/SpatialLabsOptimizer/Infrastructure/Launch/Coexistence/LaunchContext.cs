using SpatialLabsOptimizer.Infrastructure.Displays;

namespace SpatialLabsOptimizer.Infrastructure.Launch.Coexistence;

public sealed record LaunchContext(
    CoexistenceLaunchPolicy Policy,
    IReadOnlyList<string> DetectedTools,
    LaunchDisplayTarget? DisplayTarget = null)
{
    public static LaunchContext Standard { get; } = new(CoexistenceLaunchPolicy.Block, []);

    public bool IsGameFirst => Policy == CoexistenceLaunchPolicy.GameFirst;
}
