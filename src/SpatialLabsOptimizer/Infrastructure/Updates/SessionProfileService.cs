using System.Text.Json;
using SpatialLabsOptimizer.Infrastructure.Data;

namespace SpatialLabsOptimizer.Infrastructure.Updates;

public sealed class SessionProfileService
{
    private readonly SqliteSettingsStore _settings;

    public SessionProfileService(SqliteSettingsStore settings)
    {
        _settings = settings;
    }

    public async Task SaveProfileAsync(
        string name,
        SessionProfileData? data = null,
        CancellationToken cancellationToken = default)
    {
        var payload = data ?? new SessionProfileData
        {
            Name = name,
            SavedAt = DateTimeOffset.UtcNow
        };
        payload = payload with { Name = name, SavedAt = DateTimeOffset.UtcNow };
        await _settings.SetAsync($"session:{name}", JsonSerializer.Serialize(payload), cancellationToken);
    }

    public async Task<SessionProfileData?> LoadProfileAsync(string name, CancellationToken cancellationToken = default)
    {
        var raw = await _settings.GetAsync($"session:{name}", cancellationToken);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (DateTimeOffset.TryParse(raw, out var legacySavedAt))
        {
            return new SessionProfileData { Name = name, SavedAt = legacySavedAt };
        }

        return JsonSerializer.Deserialize<SessionProfileData>(raw);
    }

    public async Task<IReadOnlyList<string>> ListProfileNamesAsync(CancellationToken cancellationToken = default)
    {
        const string prefix = "session:";
        var keys = await _settings.ListKeysByPrefixAsync(prefix, cancellationToken);
        return keys
            .Where(k => k.StartsWith(prefix, StringComparison.Ordinal))
            .Select(k => k[prefix.Length..])
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<DateTimeOffset?> GetProfileSavedAtAsync(string name, CancellationToken cancellationToken = default)
    {
        var profile = await LoadProfileAsync(name, cancellationToken);
        return profile?.SavedAt;
    }
}

public sealed record SessionProfileData
{
    public string Name { get; init; } = "";
    public double Depth { get; init; } = 0.65;
    public double Convergence { get; init; } = 0.5;
    public string Theme { get; init; } = "system";
    public DateTimeOffset SavedAt { get; init; }
}
