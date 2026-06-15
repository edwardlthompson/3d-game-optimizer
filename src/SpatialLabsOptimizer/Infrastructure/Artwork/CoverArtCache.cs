namespace SpatialLabsOptimizer.Infrastructure.Artwork;

public sealed class CoverArtCache
{
    private readonly string _cacheDir;

    public CoverArtCache(string? cacheDir = null)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _cacheDir = cacheDir ?? Path.Combine(appData, "3d-game-optimizer", "cache", "covers");
        Directory.CreateDirectory(_cacheDir);
    }

    public string GetCachePath(int appId) => Path.Combine(_cacheDir, $"{appId}.jpg");

    public bool TryGetCached(int appId, out string path)
    {
        path = GetCachePath(appId);
        return File.Exists(path);
    }
}
