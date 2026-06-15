using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Launch;

namespace SpatialLabsOptimizer.Infrastructure.Install;

public sealed partial class ToolInstallDetector
{
    private readonly JsonDataLoader _loader;
    private readonly ToolPathResolver _toolPaths;
    private readonly SqliteSettingsStore _settings;
    private ToolManifestDocument? _cachedManifest;

    public ToolInstallDetector(JsonDataLoader loader, ToolPathResolver toolPaths, SqliteSettingsStore settings)
    {
        _loader = loader;
        _toolPaths = toolPaths;
        _settings = settings;
    }

    public async Task<bool> IsInstalledAsync(string toolId, CancellationToken cancellationToken = default)
    {
        var manifest = await LoadManifestAsync(cancellationToken);
        return await IsInstalledAsync(toolId, manifest, cancellationToken);
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
                await IsInstalledAsync(toolId, manifest, cancellationToken)));
        }

        return results;
    }

    public async Task<string?> GetDetectionNoteAsync(string toolId, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(toolId, "spatiallabs-runtime-platform", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var manifest = await LoadManifestAsync(cancellationToken);
        var tool = manifest?.Tools?.FirstOrDefault(t =>
            string.Equals(t.Id, toolId, StringComparison.OrdinalIgnoreCase));
        if (tool?.Verification is null)
        {
            return null;
        }

        if (IsRegistryKeyPresent(tool.Verification.PathHint))
        {
            return "Runtime registry";
        }

        if (IsExperienceCenterPresent())
        {
            return "Experience Center";
        }

        if (await HasCustomInstallPathAsync(toolId, cancellationToken))
        {
            return "Custom path";
        }

        return null;
    }

    private async Task<bool> HasCustomInstallPathAsync(string toolId, CancellationToken cancellationToken = default)
    {
        var path = await _settings.GetToolInstallPathAsync(toolId, cancellationToken);
        return !string.IsNullOrWhiteSpace(path) && Directory.Exists(path);
    }

    private async Task<bool> IsInstalledAsync(
        string toolId,
        ToolManifestDocument? manifest,
        CancellationToken cancellationToken = default)
    {
        if (await HasCustomInstallPathAsync(toolId, cancellationToken))
        {
            return true;
        }

        if (string.Equals(toolId, "spatiallabs-runtime-platform", StringComparison.OrdinalIgnoreCase))
        {
            return IsSpatialLabsRuntimeInstalled(manifest);
        }

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
}

public sealed record ToolInstallStatus(string ToolId, string Name, bool IsInstalled);
