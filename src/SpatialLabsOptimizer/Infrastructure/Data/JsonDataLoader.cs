using System.Text.Json;

namespace SpatialLabsOptimizer.Infrastructure.Data;

public sealed class JsonDataLoader
{
    public static string UserCatalogDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "3d-game-optimizer",
        "catalog");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string _dataRoot;

    public JsonDataLoader(string? dataRoot = null)
    {
        if (dataRoot is not null)
        {
            _dataRoot = dataRoot;
            return;
        }

        var candidates = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "data"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "data")
        };

        _dataRoot = candidates.Select(Path.GetFullPath).FirstOrDefault(Directory.Exists)
            ?? candidates[0];
    }

    public string DataRoot => _dataRoot;

    public async Task<T?> LoadAsync<T>(string relativePath, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(relativePath);
        if (!File.Exists(fullPath))
        {
            return default;
        }

        await using var stream = File.OpenRead(fullPath);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);
    }

    public T? Load<T>(string relativePath)
    {
        var fullPath = ResolvePath(relativePath);
        if (!File.Exists(fullPath))
        {
            return default;
        }

        var json = File.ReadAllText(fullPath);
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    private string ResolvePath(string relativePath)
    {
        if (string.Equals(relativePath, "compatibility/catalog-v2.json", StringComparison.OrdinalIgnoreCase))
        {
            var userCatalog = Path.Combine(UserCatalogDirectory, "catalog-v2.json");
            if (File.Exists(userCatalog))
            {
                return userCatalog;
            }
        }

        return Path.GetFullPath(Path.Combine(_dataRoot, relativePath));
    }
}
