namespace SpatialLabsOptimizer.Infrastructure.Install;

public interface IElevatedHelperLocator
{
    string HelperPath { get; }
}

public sealed class DefaultElevatedHelperLocator : IElevatedHelperLocator
{
    public string HelperPath =>
        Path.Combine(AppContext.BaseDirectory, "SpatialLabsOptimizer.ElevatedHelper.exe");
}
