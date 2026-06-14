using System.Collections.Concurrent;
using SpatialLabsOptimizer.Infrastructure.Data;

namespace SpatialLabsOptimizer.Infrastructure.Launch;

public sealed class LocalGameInstallResolver : IGameInstallPathResolver
{
    private readonly GameDatabase _database;
    private readonly IGameInstallPathResolver _inner;
    private readonly ConcurrentDictionary<int, GameInstallInfo> _cache = new();

    public LocalGameInstallResolver(GameDatabase database, GameInstallPathResolver inner)
    {
        _database = database;
        _inner = inner;
    }

    public void RefreshCache(IEnumerable<LocalGameInstallRecord> installs)
    {
        _cache.Clear();
        foreach (var install in installs.Where(i => !i.IsStale && File.Exists(i.LaunchExe)))
        {
            _cache[install.StableAppId] = new GameInstallInfo(
                install.FolderPath,
                install.LaunchExe);
        }
    }

    public async Task WarmCacheAsync(CancellationToken cancellationToken = default)
    {
        await _database.InitializeAsync(cancellationToken);
        RefreshCache(await _database.GetLocalInstallsAsync(cancellationToken));
    }

    public GameInstallInfo? Resolve(int steamAppId)
    {
        if (_cache.TryGetValue(steamAppId, out var cached))
        {
            return cached;
        }

        var local = _database.GetLocalInstallAsync(steamAppId).GetAwaiter().GetResult();
        if (local is not null && !local.IsStale && File.Exists(local.LaunchExe))
        {
            var info = new GameInstallInfo(local.FolderPath, local.LaunchExe);
            _cache[steamAppId] = info;
            return info;
        }

        return _inner.Resolve(steamAppId);
    }

    public string? FindSteamExecutable() => _inner.FindSteamExecutable();
}
