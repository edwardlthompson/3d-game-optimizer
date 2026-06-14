using System.Diagnostics;
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
            gameOverride?.SafeLaunch ?? false);
    }
}

public sealed class GameOverrideRepository
{
    private readonly SqliteSettingsStore _settings;

    public GameOverrideRepository(SqliteSettingsStore settings)
    {
        _settings = settings;
    }

    public Task<GameOverride?> GetAsync(int appId, CancellationToken cancellationToken = default)
        => Task.FromResult<GameOverride?>(null);

    public Task SaveAsync(GameOverride entry, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

public sealed record GameOverride(
    int SteamAppId,
    double? Depth,
    double? Convergence,
    LaunchPlatform? PlatformOverride,
    bool SafeLaunch);

public sealed class LaunchErrorCatalog
{
    private readonly Dictionary<string, (string Message, string Recovery)> _errors = new()
    {
        ["3DGO-0001"] = ("Preset cache failed", "Retry preset download"),
        ["3DGO-0002"] = ("Toolchain missing", "Re-run setup wizard"),
        ["3DGO-0003"] = ("Launch blocked — unsupported tier", "View compatibility notes"),
        ["3DGO-0004"] = ("Trainer conflict detected", "Enable coexistence or use Safe launch"),
        ["3DGO-0005"] = ("Config apply failed", "Rollback and retry")
    };

    public (string Message, string Recovery) Get(string code) =>
        _errors.TryGetValue(code, out var entry) ? entry : ("Unknown error", "Open logs");
}

public sealed class SafeLaunchService
{
    public Task<bool> LaunchAsync(int appId, CancellationToken cancellationToken = default)
        => Task.FromResult(true);
}

public sealed class TrainerCoexistenceService
{
    public bool IsTrainerRunning() =>
        Process.GetProcessesByName("WeMod").Length > 0 ||
        Process.GetProcessesByName("Wand").Length > 0;

    public Task PrepareCoexistenceAsync(CancellationToken cancellationToken = default)
        => Task.Delay(50, cancellationToken);
}

public sealed class ModManagerCoexistenceService
{
    public bool IsModManagerRunning() =>
        Process.GetProcessesByName("Vortex").Length > 0 ||
        Process.GetProcessesByName("ModOrganizer").Length > 0;
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

    public async Task RollbackAsync(string snapshotPath, CancellationToken cancellationToken = default)
    {
        if (File.Exists(snapshotPath))
        {
            await Task.CompletedTask;
        }
    }
}

public abstract class LaunchAdapterBase
{
    public abstract LaunchPlatform Platform { get; }
    public abstract Task<bool> LaunchAsync(ResolvedGameLaunchPlan plan, CancellationToken cancellationToken = default);
}

public sealed class TrueGameLauncher : LaunchAdapterBase
{
    public override LaunchPlatform Platform => LaunchPlatform.TrueGame;
    public override Task<bool> LaunchAsync(ResolvedGameLaunchPlan plan, CancellationToken cancellationToken = default)
        => Task.FromResult(true);
}

public sealed class UevrLauncher : LaunchAdapterBase
{
    public override LaunchPlatform Platform => LaunchPlatform.Uevr;
    public override Task<bool> LaunchAsync(ResolvedGameLaunchPlan plan, CancellationToken cancellationToken = default)
        => Task.FromResult(true);
}

public sealed class ReShadeLauncher : LaunchAdapterBase
{
    public override LaunchPlatform Platform => LaunchPlatform.ReShade;
    public override Task<bool> LaunchAsync(ResolvedGameLaunchPlan plan, CancellationToken cancellationToken = default)
        => Task.FromResult(true);
}

public sealed class LaunchAdapterRegistry
{
    private readonly IReadOnlyList<LaunchAdapterBase> _adapters;

    public LaunchAdapterRegistry(IEnumerable<LaunchAdapterBase> adapters)
    {
        _adapters = adapters.ToList();
    }

    public LaunchAdapterBase? GetAdapter(LaunchPlatform platform) =>
        _adapters.FirstOrDefault(a => a.Platform == platform);
}
