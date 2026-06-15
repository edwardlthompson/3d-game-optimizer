using System.Text.Json;
using Microsoft.Win32;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Launch;

namespace SpatialLabsOptimizer.Infrastructure.Install;

public sealed class ToolInstallDetector
{
    private const int ProgramFilesSubdirScanLimit = 8;

    private readonly JsonDataLoader _loader;
    private readonly ToolPathResolver _toolPaths;
    private ToolManifestDocument? _cachedManifest;

    public ToolInstallDetector(JsonDataLoader loader, ToolPathResolver toolPaths)
    {
        _loader = loader;
        _toolPaths = toolPaths;
    }

    public async Task<bool> IsInstalledAsync(string toolId, CancellationToken cancellationToken = default)
    {
        var manifest = await LoadManifestAsync(cancellationToken);
        return IsInstalled(toolId, manifest);
    }

    public async Task<IReadOnlyList<ToolInstallStatus>> GetStatusesAsync(
        IReadOnlyList<string> toolIds,
        CancellationToken cancellationToken = default)
    {
        var manifest = await LoadManifestAsync(cancellationToken);
        var names = manifest?.Tools?.ToDictionary(t => t.Id, t => t.Name, StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var results = new List<ToolInstallStatus>();
        foreach (var toolId in toolIds)
        {
            results.Add(new ToolInstallStatus(
                toolId,
                names.TryGetValue(toolId, out var name) ? name : toolId,
                IsInstalled(toolId, manifest)));
        }

        return results;
    }

    private async Task<ToolManifestDocument?> LoadManifestAsync(CancellationToken cancellationToken)
    {
        if (_cachedManifest is not null)
        {
            return _cachedManifest;
        }

        _cachedManifest = await _loader.LoadAsync<ToolManifestDocument>(
            "tools/tool-manifest-v1.json",
            cancellationToken);
        return _cachedManifest;
    }

    private bool IsInstalled(string toolId, ToolManifestDocument? manifest)
    {
        var tool = manifest?.Tools?.FirstOrDefault(t =>
            string.Equals(t.Id, toolId, StringComparison.OrdinalIgnoreCase));
        if (tool?.Verification is null)
        {
            return IsToolDirectoryPresent(toolId);
        }

        return tool.Verification.Type switch
        {
            "fileExists" => IsFilePresent(toolId, tool.Verification.PathHint),
            "registryKey" => IsRegistryKeyPresent(tool.Verification.PathHint),
            "processOrFile" => IsProcessOrFilePresent(toolId, tool.Verification.PathHint),
            _ => IsToolDirectoryPresent(toolId)
        };
    }

    private bool IsFilePresent(string toolId, string? pathHint)
    {
        if (string.IsNullOrWhiteSpace(pathHint))
        {
            return false;
        }

        return _toolPaths.ResolveExecutable(toolId, pathHint) is not null
            || _toolPaths.ResolveExecutable(toolId, pathHint, $"bin/{pathHint}") is not null;
    }

    private static bool IsRegistryKeyPresent(string? pathHint)
    {
        if (string.IsNullOrWhiteSpace(pathHint))
        {
            return false;
        }

        try
        {
            var parts = pathHint.Split('\\', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                return false;
            }

            var hive = parts[0].ToUpperInvariant() switch
            {
                "HKLM" => RegistryHive.LocalMachine,
                "HKCU" => RegistryHive.CurrentUser,
                _ => RegistryHive.LocalMachine
            };
            using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            using var key = baseKey.OpenSubKey(parts[1]);
            return key is not null;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private bool IsProcessOrFilePresent(string toolId, string? pathHint)
    {
        if (string.IsNullOrWhiteSpace(pathHint))
        {
            return false;
        }

        if (IsFilePresent(toolId, pathHint))
        {
            return true;
        }

        var localTools = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "3d-game-optimizer",
            "tools",
            toolId);
        if (Directory.Exists(localTools) &&
            Directory.EnumerateFiles(localTools, pathHint, SearchOption.AllDirectories).Any())
        {
            return true;
        }

        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        if (!Directory.Exists(programFiles))
        {
            return false;
        }

        try
        {
            return Directory.EnumerateFiles(programFiles, pathHint, SearchOption.TopDirectoryOnly).Any()
                || Directory.EnumerateDirectories(programFiles)
                    .Take(ProgramFilesSubdirScanLimit)
                    .Any(dir => Directory.EnumerateFiles(dir, pathHint, SearchOption.TopDirectoryOnly).Any());
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }

    private static bool IsToolDirectoryPresent(string toolId)
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "3d-game-optimizer",
            "tools",
            toolId);
        return Directory.Exists(path) && Directory.EnumerateFileSystemEntries(path).Any();
    }
}

public sealed record ToolInstallStatus(string ToolId, string Name, bool IsInstalled);
