using SpatialLabsOptimizer.Infrastructure.Displays;

namespace SpatialLabsOptimizer.Tests;

internal sealed class FakeDisplayEdidProbe : IDisplayEdidProbe
{
    private readonly Func<IReadOnlyList<DisplayEdidSnapshot>> _provider;

    public FakeDisplayEdidProbe(Func<IReadOnlyList<DisplayEdidSnapshot>> provider)
    {
        _provider = provider;
    }

    public IReadOnlyList<DisplayEdidSnapshot> GetCurrentSnapshots() => _provider();
}
