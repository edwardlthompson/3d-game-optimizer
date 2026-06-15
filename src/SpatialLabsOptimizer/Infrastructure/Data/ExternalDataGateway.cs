using System.Net.Http.Headers;
using SpatialLabsOptimizer.Infrastructure.Privacy;
using SpatialLabsOptimizer.Infrastructure.Progress;

namespace SpatialLabsOptimizer.Infrastructure.Data;

public sealed class ExternalDataGateway
{
    private readonly HttpClient _httpClient;
    private readonly OperationProgressHub _progressHub;
    private readonly SemaphoreSlim _rateLimiter = new(1, 1);
    private DateTimeOffset _lastRequest = DateTimeOffset.MinValue;
    private readonly TimeSpan _minDelay = TimeSpan.FromMilliseconds(500);

    public ExternalDataGateway(PrivacyGuardHttpHandler handler, OperationProgressHub progressHub)
    {
        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        _httpClient.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("3DGameOptimizer", "1.0"));
        _progressHub = progressHub;
    }

    public async Task<string?> GetStringAsync(
        string url,
        string operationId,
        CancellationToken cancellationToken = default,
        string? userMessage = null)
    {
        var (body, _) = await TryGetStringAsync(url, operationId, cancellationToken, userMessage);
        return body;
    }

    public async Task<(string? Body, int StatusCode)> TryGetStringAsync(
        string url,
        string operationId,
        CancellationToken cancellationToken = default,
        string? userMessage = null)
    {
        await _rateLimiter.WaitAsync(cancellationToken);
        try
        {
            var elapsed = DateTimeOffset.UtcNow - _lastRequest;
            if (elapsed < _minDelay)
            {
                await Task.Delay(_minDelay - elapsed, cancellationToken);
            }

            var step = userMessage ?? $"GET {new Uri(url).Host}";
            _progressHub.Publish(new OperationProgressReport(
                operationId,
                Application.Progress.OperationCategory.Download,
                "Fetching data",
                step,
                PercentComplete: null));

            _lastRequest = DateTimeOffset.UtcNow;
            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return (null, (int)response.StatusCode);
            }

            return (await response.Content.ReadAsStringAsync(cancellationToken), (int)response.StatusCode);
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    public async Task<byte[]?> GetBytesAsync(
        string url,
        string operationId,
        IProgress<(long transferred, long total)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var (bytes, _) = await TryGetBytesAsync(url, operationId, progress, cancellationToken);
        return bytes;
    }

    public async Task<(byte[]? Bytes, int StatusCode)> TryGetBytesAsync(
        string url,
        string operationId,
        IProgress<(long transferred, long total)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        await _rateLimiter.WaitAsync(cancellationToken);
        try
        {
            _lastRequest = DateTimeOffset.UtcNow;
            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return (null, (int)response.StatusCode);
            }

            var total = response.Content.Headers.ContentLength ?? -1;
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var ms = new MemoryStream();
            var buffer = new byte[8192];
            long transferred = 0;
            int read;
            while ((read = await stream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await ms.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                transferred += read;
                progress?.Report((transferred, total));
                if (total > 0)
                {
                    _progressHub.Publish(new OperationProgressReport(
                        operationId,
                        Application.Progress.OperationCategory.Download,
                        "Downloading",
                        Path.GetFileName(url),
                        PercentComplete: transferred * 100.0 / total,
                        BytesTransferred: transferred,
                        BytesTotal: total));
                }
            }

            return (ms.ToArray(), (int)response.StatusCode);
        }
        finally
        {
            _rateLimiter.Release();
        }
    }
}
