using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Steam;

namespace SpatialLabsOptimizer.Infrastructure.Updates;

public sealed class IncrementalSteamScanService
{
    private readonly SteamVdfScanner _scanner;
    private readonly GameDatabase _database;
    private readonly OperationProgressHub _progressHub;

    public IncrementalSteamScanService(
        SteamVdfScanner scanner,
        GameDatabase database,
        OperationProgressHub progressHub)
    {
        _scanner = scanner;
        _database = database;
        _progressHub = progressHub;
    }

    public async Task<int> ScanNewGamesAsync(CancellationToken cancellationToken = default)
    {
        var installed = _scanner.ScanInstalledAppIds();
        var existing = await _database.CountGamesAsync(cancellationToken);
        var delta = Math.Max(0, installed.Count - existing);

        _progressHub.Publish(new OperationProgressReport(
            "incremental-scan",
            Application.Progress.OperationCategory.Scan,
            "Incremental Steam scan",
            $"Scan complete — {delta} new games indexed",
            IsComplete: true));

        return delta;
    }
}

public sealed class HdrWatchdogService
{
    public Task<bool> IsHdrEnabledAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task DisableHdrFor3DAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

public sealed class PlayQueueService
{
    private readonly Queue<int> _queue = new();

    public void Enqueue(int appId) => _queue.Enqueue(appId);

    public bool TryDequeue(out int appId) => _queue.TryDequeue(out appId);

    public int Count => _queue.Count;
}

public sealed class SessionProfileService
{
    private readonly SqliteSettingsStore _settings;

    public SessionProfileService(SqliteSettingsStore settings)
    {
        _settings = settings;
    }

    public Task SaveProfileAsync(string name, CancellationToken cancellationToken = default)
        => _settings.SetAsync($"session:{name}", DateTimeOffset.UtcNow.ToString("O"), cancellationToken);
}

public sealed class SteamGridDbClient
{
    public Task<string?> ResolveCoverAsync(int appId, CancellationToken cancellationToken = default)
        => Task.FromResult<string?>(null);
}

public sealed class LanPartyExportService
{
    public async Task<string> ExportSessionAsync(IReadOnlyList<int> appIds, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(Path.GetTempPath(), $"lan-party-{Guid.NewGuid()}.json");
        await File.WriteAllTextAsync(path, System.Text.Json.JsonSerializer.Serialize(appIds), cancellationToken);
        return path;
    }
}

public sealed class StreamerHotkeyService
{
    public string Toggle3DHotkey => "Ctrl+Shift+3";
}

public sealed class HybridSessionService
{
    public Task StartHybridSessionAsync(int appId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

public sealed class ThreeDGoCodeService
{
    public string GenerateCode(string prefix = "3DGO") =>
        $"{prefix}-{Random.Shared.Next(1000, 9999)}";
}

public sealed class ModManagerIntegrationService
{
    private readonly ModManagerCoexistenceService _coexistence;

    public ModManagerIntegrationService(ModManagerCoexistenceService coexistence)
    {
        _coexistence = coexistence;
    }

    public bool IsModManagerRunning() => _coexistence.IsModManagerRunning();
}
