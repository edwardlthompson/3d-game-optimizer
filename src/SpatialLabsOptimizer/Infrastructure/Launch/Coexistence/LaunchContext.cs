namespace SpatialLabsOptimizer.Infrastructure.Launch.Coexistence;

public sealed record LaunchContext(
    CoexistenceLaunchPolicy Policy,
    IReadOnlyList<string> DetectedTools)
{
    public static LaunchContext Standard { get; } = new(CoexistenceLaunchPolicy.Block, []);

    public bool IsGameFirst => Policy == CoexistenceLaunchPolicy.GameFirst;
}
