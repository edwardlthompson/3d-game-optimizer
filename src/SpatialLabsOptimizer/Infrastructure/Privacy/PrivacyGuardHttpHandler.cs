namespace SpatialLabsOptimizer.Infrastructure.Privacy;

public sealed class PrivacyGuardHttpHandler : DelegatingHandler
{
    private readonly PrivacyGuard _privacyGuard;

    public PrivacyGuardHttpHandler(PrivacyGuard privacyGuard)
    {
        _privacyGuard = privacyGuard;
        InnerHandler = new SocketsHttpHandler();
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var host = request.RequestUri?.Host;
        if (string.IsNullOrWhiteSpace(host) || !_privacyGuard.IsHostAllowed(host))
        {
            throw new HttpRequestException("Request blocked by privacy guard policy.");
        }

        return base.SendAsync(request, cancellationToken);
    }
}
