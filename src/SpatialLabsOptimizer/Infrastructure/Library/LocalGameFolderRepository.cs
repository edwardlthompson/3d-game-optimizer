using System.Text.Json;
using SpatialLabsOptimizer.Infrastructure.Data;

namespace SpatialLabsOptimizer.Infrastructure.Library;

public sealed class LocalGameFolderRepository
{
    public const string SettingsKey = "local_game_folders";

    private readonly SqliteSettingsStore _settings;

    public LocalGameFolderRepository(SqliteSettingsStore settings)
    {
        _settings = settings;
    }

    public async Task<IReadOnlyList<string>> GetFoldersAsync(CancellationToken cancellationToken = default)
    {
        var raw = await _settings.GetAsync(SettingsKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<string>();
        }

        try
        {
            var folders = JsonSerializer.Deserialize<List<string>>(raw)?
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(NormalizePath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            return folders ?? [];
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }

    public async Task SetFoldersAsync(IReadOnlyList<string> folders, CancellationToken cancellationToken = default)
    {
        var normalized = folders
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(NormalizePath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        await _settings.SetAsync(SettingsKey, JsonSerializer.Serialize(normalized), cancellationToken);
    }

    public async Task AddFolderAsync(string folderPath, CancellationToken cancellationToken = default)
    {
        var folders = (await GetFoldersAsync(cancellationToken)).ToList();
        var normalized = NormalizePath(folderPath);
        if (!folders.Contains(normalized, StringComparer.OrdinalIgnoreCase))
        {
            folders.Add(normalized);
            await SetFoldersAsync(folders, cancellationToken);
        }
    }

    public async Task RemoveFolderAsync(string folderPath, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizePath(folderPath);
        var folders = (await GetFoldersAsync(cancellationToken))
            .Where(p => !string.Equals(p, normalized, StringComparison.OrdinalIgnoreCase))
            .ToList();
        await SetFoldersAsync(folders, cancellationToken);
    }

    public static string NormalizePath(string path) =>
        Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
