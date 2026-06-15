using System.Text.Json;
using SpatialLabsOptimizer.Infrastructure.Data;

namespace SpatialLabsOptimizer.Infrastructure.Updates;

public sealed class HybridSessionService
{
    private readonly SqliteSettingsStore _settings;
    private readonly ThreeDGoCodeService _codes;

    public HybridSessionService(SqliteSettingsStore settings, ThreeDGoCodeService codes)
    {
        _settings = settings;
        _codes = codes;
    }

    public async Task<HybridSession> CreateSessionAsync(int appId, CancellationToken cancellationToken = default)
    {
        var session = new HybridSession
        {
            SessionCode = _codes.GenerateCode(),
            HostAppId = appId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _settings.SetAsync("hybrid:session", JsonSerializer.Serialize(session), cancellationToken);
        return session;
    }

    public async Task<HybridSession?> GetActiveSessionAsync(CancellationToken cancellationToken = default)
    {
        var raw = await _settings.GetAsync("hybrid:session", cancellationToken);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return JsonSerializer.Deserialize<HybridSession>(raw);
    }

    public async Task StartHybridSessionAsync(int appId, CancellationToken cancellationToken = default)
    {
        await CreateSessionAsync(appId, cancellationToken);
    }
}

public sealed class HybridSession
{
    public string SessionCode { get; set; } = "";
    public int HostAppId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class ThreeDGoCodeService
{
    public string GenerateCode(string prefix = "3DGO") =>
        $"{prefix}-{Random.Shared.Next(1000, 9999)}";
}
