using System.Text.Json;
using SpatialLabsOptimizer.Infrastructure.Compatibility;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Progress;

namespace SpatialLabsOptimizer.Infrastructure.Launch;

public sealed class PresetCacheService
{
    private readonly JsonDataLoader _loader;
    private readonly ExternalDataGateway _gateway;
    private readonly string _presetDir;

    public PresetCacheService(JsonDataLoader loader, ExternalDataGateway gateway)
    {
        _loader = loader;
        _gateway = gateway;
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _presetDir = Path.Combine(appData, "3d-game-optimizer", "presets");
        Directory.CreateDirectory(_presetDir);
    }

    public async Task<bool> HasPresetAsync(int appId, CancellationToken cancellationToken = default)
    {
        return File.Exists(GetPresetPath(appId)) || await FindManifestPresetAsync(appId, cancellationToken) is not null;
    }

    public async Task CachePresetAsync(int appId, CancellationToken cancellationToken = default)
    {
        var preset = await FindManifestPresetAsync(appId, cancellationToken);
        if (preset is null)
        {
            await WritePresetTextAsync(GetPresetPath(appId), "{}", cancellationToken);
            return;
        }

        if (string.IsNullOrWhiteSpace(preset.Url))
        {
            await WritePresetTextAsync(GetPresetPath(appId), "{}", cancellationToken);
            return;
        }

        var bytes = await _gateway.GetBytesAsync(preset.Url, $"preset-{appId}", null, cancellationToken);
        if (bytes is not null)
        {
            await WritePresetBytesAsync(GetPresetPath(appId), bytes, cancellationToken);
        }
    }

    public async Task BulkCacheTopPresetsAsync(int maxCount, OperationProgressHub hub, CancellationToken cancellationToken = default)
    {
        var manifest = await _loader.LoadAsync<PresetManifest>("presets/preset-manifest-v1.json", cancellationToken);
        var presets = manifest?.UevrProfiles?.Take(maxCount).ToList() ?? [];
        if (presets.Count == 0)
        {
            hub.Publish(new OperationProgressReport(
                "bulk-preset",
                Application.Progress.OperationCategory.Download,
                "Caching presets",
                "No UEVR presets in manifest",
                IsComplete: true));
            return;
        }

        for (var i = 0; i < presets.Count; i++)
        {
            var preset = presets[i];
            var appId = preset.SteamAppIds.FirstOrDefault();
            if (appId > 0)
            {
                await CachePresetAsync(appId, cancellationToken);
            }

            hub.Publish(new OperationProgressReport(
                "bulk-preset",
                Application.Progress.OperationCategory.Download,
                "Caching presets",
                preset.Id,
                StepIndex: i + 1,
                TotalSteps: presets.Count,
                PercentComplete: (i + 1) * 100.0 / presets.Count));
        }
    }

    public TimeSpan? GetCachedPresetAge(int appId)
    {
        var path = GetPresetPath(appId);
        if (!File.Exists(path))
        {
            return null;
        }

        return DateTimeOffset.UtcNow - File.GetLastWriteTimeUtc(path);
    }

    private string GetPresetPath(int appId) => Path.Combine(_presetDir, $"{appId}.json");

    private static async Task WritePresetTextAsync(string path, string content, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(content.AsMemory(), cancellationToken);
    }

    private static async Task WritePresetBytesAsync(string path, byte[] bytes, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        await stream.WriteAsync(bytes, cancellationToken);
    }

    private async Task<PresetEntry?> FindManifestPresetAsync(int appId, CancellationToken cancellationToken)
    {
        var manifest = await _loader.LoadAsync<PresetManifest>("presets/preset-manifest-v1.json", cancellationToken);
        return manifest?.UevrProfiles?.FirstOrDefault(p => p.SteamAppIds.Contains(appId))
            ?? manifest?.ReshadePresets?.FirstOrDefault(p => p.SteamAppIds.Contains(appId));
    }

    private sealed class PresetManifest
    {
        public List<PresetEntry>? UevrProfiles { get; set; }
        public List<PresetEntry>? ReshadePresets { get; set; }
    }

    private sealed class PresetEntry
    {
        public string Id { get; set; } = "";
        public string Url { get; set; } = "";
        public string Sha256 { get; set; } = "";
        public List<int> SteamAppIds { get; set; } = [];
    }
}
