using System.Text.Json;

namespace SpatialLabsOptimizer.Infrastructure.Data;

public sealed class JsonDataLoader
{
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
        var fullPath = Path.GetFullPath(Path.Combine(_dataRoot, relativePath));
        if (!File.Exists(fullPath))
        {
            return default;
        }

        await using var stream = File.OpenRead(fullPath);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);
    }

    public T? Load<T>(string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_dataRoot, relativePath));
        if (!File.Exists(fullPath))
        {
            return default;
        }

        var json = File.ReadAllText(fullPath);
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }
}
