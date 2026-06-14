using System.Text.Json;
using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Compatibility;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Install;
using SpatialLabsOptimizer.Infrastructure.Progress;

namespace SpatialLabsOptimizer.Infrastructure.Launch;

public sealed class LaunchReadinessService
{
    private readonly PresetCacheService _presetCache;

    public LaunchReadinessService(PresetCacheService presetCache)
    {
        _presetCache = presetCache;
    }

    public async Task<LaunchReadinessState> EvaluateAsync(int appId, bool isInstalled, CompatibilityTier tier, CancellationToken cancellationToken = default)
    {
        if (tier >= CompatibilityTier.Unsupported)
        {
            return LaunchReadinessState.Blocked;
        }

        if (!isInstalled)
        {
            return LaunchReadinessState.NeedsInstall;
        }

        if (!await _presetCache.HasPresetAsync(appId, cancellationToken))
        {
            return LaunchReadinessState.NeedsPresetCache;
        }

        return LaunchReadinessState.Ready;
    }
}

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
            await File.WriteAllTextAsync(GetPresetPath(appId), "{}", cancellationToken);
            return;
        }

        if (string.IsNullOrWhiteSpace(preset.Url))
        {
            await File.WriteAllTextAsync(GetPresetPath(appId), "{}", cancellationToken);
            return;
        }

        var bytes = await _gateway.GetBytesAsync(preset.Url, $"preset-{appId}", null, cancellationToken);
        if (bytes is not null)
        {
            await File.WriteAllBytesAsync(GetPresetPath(appId), bytes, cancellationToken);
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

public sealed class LaunchPlatformRouter
{
    public LaunchPlatform Route(
        CompatibilityTier tier,
        IDisplayVendorAdapter vendorAdapter,
        LaunchPlatform? platformOverride = null)
    {
        if (platformOverride.HasValue)
        {
            return platformOverride.Value;
        }

        if (tier >= CompatibilityTier.Unsupported)
        {
            return LaunchPlatform.Blocked;
        }

        return vendorAdapter.GetPreferredLaunchPlatform(tier);
    }
}

public sealed class ResolveGameSettings
{
    private readonly OptimalDefaultsService _defaults;
    private readonly GameOverrideRepository _overrides;
    private readonly CompatibilityRepository _compatibility;
    private readonly LaunchPlatformRouter _router;

    public ResolveGameSettings(
        OptimalDefaultsService defaults,
        GameOverrideRepository overrides,
        CompatibilityRepository compatibility,
        LaunchPlatformRouter router)
    {
        _defaults = defaults;
        _overrides = overrides;
        _compatibility = compatibility;
        _router = router;
    }

    public async Task<ResolvedGameLaunchPlan> ResolveAsync(
        int appId,
        IDisplayVendorAdapter vendorAdapter,
        CancellationToken cancellationToken = default)
    {
        var entry = await _compatibility.GetByAppIdAsync(appId, cancellationToken);
        var tier = entry is not null
            ? _compatibility.GetTierForVendor(entry, vendorAdapter.Vendor)
            : CompatibilityTier.Experimental;

        var gameOverride = await _overrides.GetAsync(appId, cancellationToken);
        var platform = _router.Route(tier, vendorAdapter, gameOverride?.PlatformOverride);
        var safeLaunch = gameOverride?.SafeLaunch ?? false;
        var preferredOutput = gameOverride?.PreferredOutput ?? "Auto";

        return new ResolvedGameLaunchPlan(
            appId,
            entry?.Title ?? $"App {appId}",
            platform,
            tier,
            gameOverride?.Depth ?? 0.65,
            gameOverride?.Convergence ?? 0.5,
            0.7,
            null,
            null,
            safeLaunch,
            preferredOutput);
    }
}

public sealed class GameOverrideRepository
{
    private readonly SqliteSettingsStore _settings;

    public GameOverrideRepository(SqliteSettingsStore settings)
    {
        _settings = settings;
    }

    public async Task<GameOverride?> GetAsync(int appId, CancellationToken cancellationToken = default)
    {
        var raw = await _settings.GetAsync($"override:{appId}", cancellationToken);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var parts = raw.Split('|');
        return new GameOverride(
            appId,
            parts.Length > 0 && double.TryParse(parts[0], out var d) ? d : null,
            parts.Length > 1 && double.TryParse(parts[1], out var c) ? c : null,
            parts.Length > 2 && Enum.TryParse<LaunchPlatform>(parts[2], out var p) ? p : null,
            parts.Length > 3 && parts[3] == "1",
            parts.Length > 4 ? parts[4] : "Auto");
    }

    public async Task SaveAsync(GameOverride entry, CancellationToken cancellationToken = default)
    {
        var raw = string.Join('|',
            entry.Depth?.ToString("F2") ?? "",
            entry.Convergence?.ToString("F2") ?? "",
            entry.PlatformOverride?.ToString() ?? "",
            entry.SafeLaunch ? "1" : "0",
            entry.PreferredOutput ?? "Auto");
        await _settings.SetAsync($"override:{entry.SteamAppId}", raw, cancellationToken);
    }
}

public sealed record GameOverride(
    int SteamAppId,
    double? Depth,
    double? Convergence,
    LaunchPlatform? PlatformOverride,
    bool SafeLaunch,
    string PreferredOutput = "Auto");

public sealed class LaunchErrorCatalog
{
    private readonly Dictionary<string, (string Message, string Recovery)> _errors = new()
    {
        ["3DGO-0001"] = ("Preset cache failed", "Retry preset download"),
        ["3DGO-0002"] = ("Toolchain missing", "Re-run setup wizard"),
        ["3DGO-0003"] = ("Launch blocked — unsupported tier", "View compatibility notes"),
        ["3DGO-0004"] = ("External tool conflict detected", "Enable coexistence or use Safe launch"),
        ["3DGO-0005"] = ("Config apply failed", "Rollback and retry")
    };

    public (string Message, string Recovery) Get(string code) =>
        _errors.TryGetValue(code, out var entry) ? entry : ("Unknown error", "Open logs");
}

public sealed class SafeLaunchService
{
    private readonly IProcessLauncher _launcher;

    public SafeLaunchService(IProcessLauncher launcher)
    {
        _launcher = launcher;
    }

    public Task<bool> LaunchAsync(int appId, CancellationToken cancellationToken = default)
        => _launcher.TryStartSteamGameAsync(appId, cancellationToken);
}

public sealed class ConfigSnapshotService
{
    private readonly string _snapshotDir;

    public ConfigSnapshotService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _snapshotDir = Path.Combine(appData, "3d-game-optimizer", "snapshots");
        Directory.CreateDirectory(_snapshotDir);
    }

    public async Task<string> SnapshotAsync(int appId, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(_snapshotDir, $"{appId}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.json");
        await File.WriteAllTextAsync(path, "{}", cancellationToken);
        return path;
    }

    public IReadOnlyList<ConfigSnapshotEntry> ListSnapshots(int? appId = null)
    {
        if (!Directory.Exists(_snapshotDir))
        {
            return [];
        }

        return Directory.EnumerateFiles(_snapshotDir, "*.json")
            .Select(path =>
            {
                var name = Path.GetFileNameWithoutExtension(path);
                var parts = name.Split('-', 2);
                var parsedAppId = parts.Length > 0 && int.TryParse(parts[0], out var id) ? id : 0;
                var created = File.GetCreationTimeUtc(path);
                return new ConfigSnapshotEntry(parsedAppId, path, new DateTimeOffset(created, TimeSpan.Zero));
            })
            .Where(entry => appId is null || entry.AppId == appId)
            .OrderByDescending(entry => entry.CreatedAt)
            .ToList();
    }

    public async Task RollbackAsync(string snapshotPath, CancellationToken cancellationToken = default)
    {
        if (File.Exists(snapshotPath))
        {
            await Task.CompletedTask;
        }
    }
}

public sealed record ConfigSnapshotEntry(int AppId, string Path, DateTimeOffset CreatedAt);
