using SpatialLabsOptimizer.Infrastructure.Data;

namespace SpatialLabsOptimizer.Infrastructure.Library;

public sealed class PinnedShelfRepository
{
    private readonly SqliteSettingsStore _settings;

    public PinnedShelfRepository(SqliteSettingsStore settings)
    {
        _settings = settings;
    }

    public async Task<IReadOnlyList<int>> GetPinnedAppIdsAsync(CancellationToken cancellationToken = default)
    {
        var raw = await _settings.GetAsync("pinned_shelf", cancellationToken);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<int>();
        }

        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s, out var id) ? id : 0)
            .Where(id => id > 0)
            .ToList();
    }

    public async Task SetPinnedAppIdsAsync(IEnumerable<int> appIds, CancellationToken cancellationToken = default)
    {
        await _settings.SetAsync("pinned_shelf", string.Join(',', appIds), cancellationToken);
    }

    public async Task RemovePinnedAppIdAsync(int appId, CancellationToken cancellationToken = default)
    {
        var pinned = (await GetPinnedAppIdsAsync(cancellationToken))
            .Where(id => id != appId)
            .ToList();
        await SetPinnedAppIdsAsync(pinned, cancellationToken);
    }

    public async Task<bool> IsPinnedAsync(int appId, CancellationToken cancellationToken = default)
    {
        var pinned = await GetPinnedAppIdsAsync(cancellationToken);
        return pinned.Contains(appId);
    }
}

public sealed class LocalPlaylistRepository
{
    private const string KeyPrefix = "playlist:";
    private readonly SqliteSettingsStore _settings;

    public LocalPlaylistRepository(SqliteSettingsStore settings)
    {
        _settings = settings;
    }

    public Task SavePlaylistAsync(string name, IReadOnlyList<int> appIds, CancellationToken cancellationToken = default)
        => _settings.SetAsync($"{KeyPrefix}{name}", string.Join(',', appIds), cancellationToken);

    public async Task<IReadOnlyList<int>> LoadPlaylistAsync(string name, CancellationToken cancellationToken = default)
    {
        var raw = await _settings.GetAsync($"{KeyPrefix}{name}", cancellationToken);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<int>();
        }

        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s, out var id) ? id : 0)
            .Where(id => id > 0)
            .ToList();
    }

    public async Task<IReadOnlyList<string>> ListPlaylistNamesAsync(CancellationToken cancellationToken = default)
    {
        var keys = await _settings.ListKeysByPrefixAsync(KeyPrefix, cancellationToken);
        return keys
            .Where(k => k.StartsWith(KeyPrefix, StringComparison.Ordinal))
            .Select(k => k[KeyPrefix.Length..])
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
