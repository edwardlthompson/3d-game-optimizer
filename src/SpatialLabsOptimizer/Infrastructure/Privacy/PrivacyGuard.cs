namespace SpatialLabsOptimizer.Infrastructure.Privacy;

public sealed class PrivacyGuard
{
    private readonly HashSet<string> _allowedHosts;

    public PrivacyGuard(IEnumerable<string> allowedHosts)
    {
        _allowedHosts = new HashSet<string>(allowedHosts, StringComparer.OrdinalIgnoreCase);
    }

    public bool IsHostAllowed(string host) => _allowedHosts.Contains(host);
}
