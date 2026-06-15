using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpatialLabsOptimizer.Infrastructure.Launch;

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

    public ConfigSnapshotService(GameOverrideRepository overrides, string? snapshotDirectory = null)
    {
        _overrides = overrides;
        _snapshotDir = snapshotDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "3d-game-optimizer",
            "snapshots");
        Directory.CreateDirectory(_snapshotDir);
    }

    public async Task<string> SnapshotAsync(int appId, CancellationToken cancellationToken = default)
    {
        var existing = await _overrides.GetAsync(appId, cancellationToken);
        var payload = ConfigSnapshotPayload.FromOverride(appId, existing);
        var path = Path.Combine(
            _snapshotDir,
            $"{appId}-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}.json");
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
                var created = ConfigSnapshotFilename.TryParseTimestamp(name)
                    ?? new DateTimeOffset(File.GetCreationTimeUtc(path), TimeSpan.Zero);
                return new ConfigSnapshotEntry(parsedAppId, path, created);
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

internal static class ConfigSnapshotFilename
{
    internal static DateTimeOffset? TryParseTimestamp(string fileNameWithoutExtension)
    {
        var firstDash = fileNameWithoutExtension.IndexOf('-', StringComparison.Ordinal);
        if (firstDash < 0)
        {
            return null;
        }

        var secondDash = fileNameWithoutExtension.IndexOf('-', firstDash + 1);
        if (secondDash < 0)
        {
            return null;
        }

        var token = fileNameWithoutExtension[(firstDash + 1)..secondDash];
        return DateTimeOffset.TryParseExact(
            token,
            "yyyyMMddHHmmssfff",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var parsed)
            ? parsed
            : null;
    }
}
