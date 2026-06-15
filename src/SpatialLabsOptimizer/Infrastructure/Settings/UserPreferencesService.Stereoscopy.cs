namespace SpatialLabsOptimizer.Infrastructure.Settings;

public sealed partial class UserPreferencesService
{
    internal const string DefaultDepthKey = "default_depth";
    internal const string DefaultConvergenceKey = "default_convergence";

    public async Task<double> GetDefaultDepthAsync(CancellationToken cancellationToken = default)
    {
        var value = await _settings.GetAsync(DefaultDepthKey, cancellationToken);
        return double.TryParse(value, out var depth) ? depth : 0.65;
    }

    public async Task SetDefaultDepthAsync(double depth, CancellationToken cancellationToken = default)
    {
        await _settings.SetAsync(DefaultDepthKey, depth.ToString("F2"), cancellationToken);
    }

    public async Task<double> GetDefaultConvergenceAsync(CancellationToken cancellationToken = default)
    {
        var value = await _settings.GetAsync(DefaultConvergenceKey, cancellationToken);
        return double.TryParse(value, out var convergence) ? convergence : 0.5;
    }

    public async Task SetDefaultConvergenceAsync(double convergence, CancellationToken cancellationToken = default)
    {
        await _settings.SetAsync(DefaultConvergenceKey, convergence.ToString("F2"), cancellationToken);
    }
}
