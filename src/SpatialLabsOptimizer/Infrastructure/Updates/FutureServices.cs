using System.Text.Json;
using Microsoft.Win32;
using SpatialLabsOptimizer.Infrastructure.Artwork;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Launch.Coexistence;
using SpatialLabsOptimizer.Infrastructure.Library;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Steam;

namespace SpatialLabsOptimizer.Infrastructure.Updates;

public sealed class IncrementalSteamScanService
{
    private readonly SteamVdfScanner _scanner;
    private readonly GameDatabase _database;
    private readonly LibraryIndexer _indexer;
    private readonly OperationProgressHub _progressHub;

    public IncrementalSteamScanService(
        SteamVdfScanner scanner,
        GameDatabase database,
        LibraryIndexer indexer,
        OperationProgressHub progressHub)
    {
        _scanner = scanner;
        _database = database;
        _indexer = indexer;
        _progressHub = progressHub;
    }

    public async Task<int> ScanNewGamesAsync(CancellationToken cancellationToken = default)
    {
        await _database.InitializeAsync(cancellationToken);
        var installed = _scanner.ScanInstalledAppIds();
        var existing = await _database.CountGamesAsync(cancellationToken);
        var delta = Math.Max(0, installed.Count - existing);

        if (delta > 0)
        {
            await _indexer.IndexAsync(cancellationToken);
        }

        _progressHub.Publish(new OperationProgressReport(
            "incremental-scan",
            Application.Progress.OperationCategory.Scan,
            "Incremental Steam scan",
            delta > 0 ? $"Indexed {delta} new game(s)" : "Library up to date",
            IsComplete: true));

        return delta;
    }
}

public sealed class HdrWatchdogService
{
    private const string HdrSettingsKey = @"Software\Microsoft\Windows\CurrentVersion\HDR";

    public Task<bool> IsHdrEnabledAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(HdrSettingsKey);
            var value = key?.GetValue("LastKnownEnabledState");
            if (value is int enabled)
            {
                return Task.FromResult(enabled == 1);
            }
        }
        catch (Exception)
        {
            // Registry unavailable — assume SDR.
        }

        return Task.FromResult(false);
    }

    public async Task<bool> DisableHdrFor3DAsync(CancellationToken cancellationToken = default)
    {
        if (!await IsHdrEnabledAsync(cancellationToken))
        {
            return false;
        }

        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(HdrSettingsKey, true);
            key?.SetValue("LastKnownEnabledState", 0, RegistryValueKind.DWord);
            return true;
        }
        catch (Exception)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var flagPath = Path.Combine(appData, "3d-game-optimizer", "config", "hdr-disable-requested.flag");
            Directory.CreateDirectory(Path.GetDirectoryName(flagPath)!);
            await File.WriteAllTextAsync(flagPath, DateTimeOffset.UtcNow.ToString("O"), cancellationToken);
            return true;
        }
    }
}

public sealed class PlayQueueService
{
    private readonly Queue<int> _queue = new();

    public void Enqueue(int appId) => _queue.Enqueue(appId);

    public bool TryDequeue(out int appId) => _queue.TryDequeue(out appId);

    public int Count => _queue.Count;

    public IReadOnlyList<int> Snapshot() => _queue.ToList();
}

public sealed class SessionProfileService
{
    private readonly SqliteSettingsStore _settings;

    public SessionProfileService(SqliteSettingsStore settings)
    {
        _settings = settings;
    }

    public async Task SaveProfileAsync(
        string name,
        SessionProfileData? data = null,
        CancellationToken cancellationToken = default)
    {
        var payload = data ?? new SessionProfileData
        {
            Name = name,
            SavedAt = DateTimeOffset.UtcNow
        };
        payload = payload with { Name = name, SavedAt = DateTimeOffset.UtcNow };
        await _settings.SetAsync($"session:{name}", JsonSerializer.Serialize(payload), cancellationToken);
    }

    public async Task<SessionProfileData?> LoadProfileAsync(string name, CancellationToken cancellationToken = default)
    {
        var raw = await _settings.GetAsync($"session:{name}", cancellationToken);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (DateTimeOffset.TryParse(raw, out var legacySavedAt))
        {
            return new SessionProfileData { Name = name, SavedAt = legacySavedAt };
        }

        return JsonSerializer.Deserialize<SessionProfileData>(raw);
    }

    public async Task<IReadOnlyList<string>> ListProfileNamesAsync(CancellationToken cancellationToken = default)
    {
        const string prefix = "session:";
        var keys = await _settings.ListKeysByPrefixAsync(prefix, cancellationToken);
        return keys
            .Where(k => k.StartsWith(prefix, StringComparison.Ordinal))
            .Select(k => k[prefix.Length..])
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<DateTimeOffset?> GetProfileSavedAtAsync(string name, CancellationToken cancellationToken = default)
    {
        var profile = await LoadProfileAsync(name, cancellationToken);
        return profile?.SavedAt;
    }
}

public sealed record SessionProfileData
{
    public string Name { get; init; } = "";
    public double Depth { get; init; } = 0.65;
    public double Convergence { get; init; } = 0.5;
    public string Theme { get; init; } = "system";
    public DateTimeOffset SavedAt { get; init; }
}

public sealed class SteamGridDbClient
{
    private readonly ExternalDataGateway _gateway;
    private readonly CoverArtCache _cache;

    public SteamGridDbClient(ExternalDataGateway gateway, CoverArtCache cache)
    {
        _gateway = gateway;
        _cache = cache;
    }

    public async Task<string?> ResolveCoverAsync(int appId, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetCached(appId, out var cached))
        {
            return cached;
        }

        var url = $"https://www.steamgriddb.com/api/v2/grids/game/{appId}";
        var json = await _gateway.GetStringAsync(url, $"steamgrid-{appId}", cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("data", out var data) || data.GetArrayLength() == 0)
        {
            return null;
        }

        var first = data[0];
        if (first.TryGetProperty("url", out var urlProp))
        {
            return urlProp.GetString();
        }

        return null;
    }
}

public sealed class LanPartyExportService
{
    public sealed record ExportEntry(int AppId, string Title);

    public async Task<string> ExportSessionAsync(
        IReadOnlyList<ExportEntry> games,
        CancellationToken cancellationToken = default)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appData, "3d-game-optimizer", "exports");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"lan-party-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.json");
        var payload = new
        {
            exportedAt = DateTimeOffset.UtcNow,
            games = games.Select(g => new { appId = g.AppId, title = g.Title }).ToList()
        };
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(payload), cancellationToken);
        return path;
    }

    public Task<string> ExportSessionAsync(IReadOnlyList<int> appIds, CancellationToken cancellationToken = default)
        => ExportSessionAsync(
            appIds.Select(id => new ExportEntry(id, $"App {id}")).ToList(),
            cancellationToken);
}

public sealed class StreamerHotkeyService
{
    public string Toggle3DHotkey => "Ctrl+Shift+3";
}

public sealed class HybridSessionService
{
    private readonly SqliteSettingsStore _settings;
    private readonly ThreeDGoCodeService _codes;

    public HybridSessionService(SqliteSettingsStore settings, ThreeDGoCodeService codes)
    {
        _settings = settings;
        _codes = codes;
    }

    public async Task<HybridSession> CreateSessionAsync(int appId, CancellationToken cancellationToken = default)
    {
        var session = new HybridSession
        {
            SessionCode = _codes.GenerateCode(),
            HostAppId = appId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _settings.SetAsync("hybrid:session", JsonSerializer.Serialize(session), cancellationToken);
        return session;
    }

    public async Task<HybridSession?> GetActiveSessionAsync(CancellationToken cancellationToken = default)
    {
        var raw = await _settings.GetAsync("hybrid:session", cancellationToken);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return JsonSerializer.Deserialize<HybridSession>(raw);
    }

    public async Task StartHybridSessionAsync(int appId, CancellationToken cancellationToken = default)
    {
        await CreateSessionAsync(appId, cancellationToken);
    }
}

public sealed class HybridSession
{
    public string SessionCode { get; set; } = "";
    public int HostAppId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class ThreeDGoCodeService
{
    public string GenerateCode(string prefix = "3DGO") =>
        $"{prefix}-{Random.Shared.Next(1000, 9999)}";
}

public sealed class ModManagerIntegrationService
{
    private readonly ExternalToolCoexistenceService _coexistence;

    public ModManagerIntegrationService(ExternalToolCoexistenceService coexistence)
    {
        _coexistence = coexistence;
    }

    public bool IsModManagerRunning() => _coexistence.GetRunningModManagers().Count > 0;
}

public sealed class WorkshopPresetImporter
{
    private readonly JsonDataLoader _loader;
    private readonly PresetCacheService _presets;
    private readonly ExternalDataGateway _gateway;
    private readonly Privacy.PrivacyGuard _privacyGuard;

    public WorkshopPresetImporter(
        JsonDataLoader loader,
        PresetCacheService presets,
        ExternalDataGateway gateway,
        Privacy.PrivacyGuard privacyGuard)
    {
        _loader = loader;
        _presets = presets;
        _gateway = gateway;
        _privacyGuard = privacyGuard;
    }

    public async Task<int> ImportAllowlistedSourcesAsync(CancellationToken cancellationToken = default)
    {
        var sources = await _loader.LoadAsync<WorkshopSourcesDocument>("presets/workshop-sources-v1.json", cancellationToken);
        var imported = 0;
        foreach (var url in sources?.AllowlistedUrls ?? [])
        {
            if (!IsAllowlistedUrl(url))
            {
                continue;
            }

            imported += await ImportPresetBundleFromUrlAsync(url, cancellationToken);
        }

        imported += await ImportFromManifestAsync(cancellationToken);
        return imported;
    }

    public async Task<int> ImportFromUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        if (!IsAllowlistedUrl(url))
        {
            return 0;
        }

        return await ImportPresetBundleFromUrlAsync(url, cancellationToken);
    }

    public async Task<int> ImportFromManifestAsync(CancellationToken cancellationToken = default)
    {
        var manifest = await _loader.LoadAsync<PresetManifestDoc>("presets/preset-manifest-v1.json", cancellationToken);
        var count = manifest?.UevrProfiles?.Count ?? 0;
        if (count > 0 && manifest!.UevrProfiles![0].SteamAppIds.Count > 0)
        {
            await _presets.CachePresetAsync(manifest.UevrProfiles[0].SteamAppIds[0], cancellationToken);
        }

        return count;
    }

    private async Task<int> ImportPresetBundleFromUrlAsync(string url, CancellationToken cancellationToken)
    {
        var json = await _gateway.GetStringAsync(url, "workshop-import", cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
        {
            return 0;
        }

        var manifest = JsonSerializer.Deserialize<PresetManifestDoc>(json);
        var profiles = manifest?.UevrProfiles ?? [];
        foreach (var profile in profiles)
        {
            var appId = profile.SteamAppIds.FirstOrDefault();
            if (appId > 0)
            {
                await _presets.CachePresetAsync(appId, cancellationToken);
            }
        }

        return profiles.Count;
    }

    private bool IsAllowlistedUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return _privacyGuard.IsHostAllowed(uri.Host);
    }

    private sealed class WorkshopSourcesDocument
    {
        public List<string>? AllowlistedUrls { get; set; }
    }

    private sealed class PresetManifestDoc
    {
        public List<PresetProfileDoc>? UevrProfiles { get; set; }
    }

    private sealed class PresetProfileDoc
    {
        public List<int> SteamAppIds { get; set; } = [];
    }
}
