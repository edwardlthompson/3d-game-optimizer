using System.Text.Json;
using System.Text.Json.Serialization;
using SpatialLabsOptimizer.Domain;

namespace SpatialLabsOptimizer.Infrastructure.Launch;

public sealed record ConfigSnapshotEntry(int AppId, string Path, DateTimeOffset CreatedAt);

public sealed class ConfigSnapshotPayload
{
    public int AppId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public SnapshotOverrideData? Override { get; init; }

    public static ConfigSnapshotPayload FromOverride(int appId, GameOverride? entry) => new()
    {
        AppId = appId,
        CreatedAt = DateTimeOffset.UtcNow,
        Override = entry is null ? null : SnapshotOverrideData.From(entry)
    };

    public GameOverride? ToOverride()
    {
        if (Override is null)
        {
            return null;
        }

        LaunchPlatform? platform = null;
        if (!string.IsNullOrWhiteSpace(Override.PlatformOverride) &&
            Enum.TryParse<LaunchPlatform>(Override.PlatformOverride, out var parsed))
        {
            platform = parsed;
        }

        return new GameOverride(
            AppId,
            Override.Depth,
            Override.Convergence,
            platform,
            Override.SafeLaunch,
            Override.PreferredOutput ?? "Auto");
    }
}

public sealed class SnapshotOverrideData
{
    public double? Depth { get; init; }
    public double? Convergence { get; init; }
    public string? PlatformOverride { get; init; }
    public bool SafeLaunch { get; init; }
    public string? PreferredOutput { get; init; }

    public static SnapshotOverrideData From(GameOverride entry) => new()
    {
        Depth = entry.Depth,
        Convergence = entry.Convergence,
        PlatformOverride = entry.PlatformOverride?.ToString(),
        SafeLaunch = entry.SafeLaunch,
        PreferredOutput = entry.PreferredOutput
    };
}

public sealed class ConfigSnapshotService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    private readonly GameOverrideRepository _overrides;
    private readonly string _snapshotDir;

    public ConfigSnapshotService(GameOverrideRepository overrides)
    {
        _overrides = overrides;
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _snapshotDir = Path.Combine(appData, "3d-game-optimizer", "snapshots");
        Directory.CreateDirectory(_snapshotDir);
    }

    public async Task<string> SnapshotAsync(int appId, CancellationToken cancellationToken = default)
    {
        var existing = await _overrides.GetAsync(appId, cancellationToken);
        var payload = ConfigSnapshotPayload.FromOverride(appId, existing);
        var path = Path.Combine(_snapshotDir, $"{appId}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(payload, JsonOptions), cancellationToken);
        return path;
    }

    public IReadOnlyList<ConfigSnapshotEntry> ListSnapshots(int? appId = null)
    {
        if (!Directory.Exists(_snapshotDir))
        {
            return [];
        }

        return Directory.EnumerateFiles(_snapshotDir, "*.json")
            .Select(path =>
            {
                var name = Path.GetFileNameWithoutExtension(path);
                var dash = name.IndexOf('-', StringComparison.Ordinal);
                var parsedAppId = dash > 0 && int.TryParse(name[..dash], out var id) ? id : 0;
                var created = File.GetCreationTimeUtc(path);
                return new ConfigSnapshotEntry(parsedAppId, path, new DateTimeOffset(created, TimeSpan.Zero));
            })
            .Where(entry => appId is null || entry.AppId == appId)
            .OrderByDescending(entry => entry.CreatedAt)
            .ToList();
    }

    public async Task RollbackAsync(string snapshotPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(snapshotPath))
        {
            return;
        }

        var json = await File.ReadAllTextAsync(snapshotPath, cancellationToken);
        if (string.IsNullOrWhiteSpace(json) || json.Trim() == "{}")
        {
            return;
        }

        var payload = JsonSerializer.Deserialize<ConfigSnapshotPayload>(json, JsonOptions);
        if (payload is null)
        {
            return;
        }

        var restored = payload.ToOverride();
        if (restored is null)
        {
            await _overrides.RemoveAsync(payload.AppId, cancellationToken);
            return;
        }

        await _overrides.SaveAsync(restored, cancellationToken);
    }
}
