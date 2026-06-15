using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Data;

namespace SpatialLabsOptimizer.Infrastructure.Launch;

public sealed class GameOverrideRepository
{
    private readonly SqliteSettingsStore _settings;

    public GameOverrideRepository(SqliteSettingsStore settings)
    {
        _settings = settings;
    }

    public async Task<GameOverride?> GetAsync(int appId, CancellationToken cancellationToken = default)
    {
        var raw = await _settings.GetAsync($"override:{appId}", cancellationToken);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var parts = raw.Split('|');
        return new GameOverride(
            appId,
            parts.Length > 0 && double.TryParse(parts[0], out var d) ? d : null,
            parts.Length > 1 && double.TryParse(parts[1], out var c) ? c : null,
            parts.Length > 2 && Enum.TryParse<LaunchPlatform>(parts[2], out var p) ? p : null,
            parts.Length > 3 && parts[3] == "1",
            parts.Length > 4 ? parts[4] : "Auto");
    }

    public async Task SaveAsync(GameOverride entry, CancellationToken cancellationToken = default)
    {
        var raw = string.Join('|',
            entry.Depth?.ToString("F2") ?? "",
            entry.Convergence?.ToString("F2") ?? "",
            entry.PlatformOverride?.ToString() ?? "",
            entry.SafeLaunch ? "1" : "0",
            entry.PreferredOutput ?? "Auto");
        await _settings.SetAsync($"override:{entry.SteamAppId}", raw, cancellationToken);
    }

    public Task RemoveAsync(int appId, CancellationToken cancellationToken = default)
        => _settings.SetAsync($"override:{appId}", "", cancellationToken);
}

public sealed record GameOverride(
    int SteamAppId,
    double? Depth,
    double? Convergence,
    LaunchPlatform? PlatformOverride,
    bool SafeLaunch,
    string PreferredOutput = "Auto");
