using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Library;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Steam;

namespace SpatialLabsOptimizer.Infrastructure.Updates;

public sealed class IncrementalSteamScanService
{
    internal const string LastScanUtcKey = "last_incremental_scan_utc";
    internal static readonly TimeSpan ScanThrottle = TimeSpan.FromMinutes(15);

    private readonly SteamVdfScanner _scanner;
    private readonly GameDatabase _database;
    private readonly LibraryIndexer _indexer;
    private readonly OperationProgressHub _progressHub;
    private readonly SqliteSettingsStore _settings;

    public IncrementalSteamScanService(
        SteamVdfScanner scanner,
        GameDatabase database,
        LibraryIndexer indexer,
        OperationProgressHub progressHub,
        SqliteSettingsStore settings)
    {
        _scanner = scanner;
        _database = database;
        _indexer = indexer;
        _progressHub = progressHub;
        _settings = settings;
    }

    public async Task<int> ScanNewGamesAsync(bool force = false, CancellationToken cancellationToken = default)
    {
        if (!force && !await ShouldScanAsync(cancellationToken))
        {
            return 0;
        }

        await _database.InitializeAsync(cancellationToken);
        var installed = _scanner.ScanInstalledAppIds().ToHashSet();
        var knownInstalled = await _database.GetInstalledSteamAppIdsAsync(cancellationToken);
        var delta = CountNewInstalls(installed, knownInstalled);

        if (delta > 0)
        {
            await _indexer.IndexAsync(cancellationToken);
        }

        await _settings.SetAsync(LastScanUtcKey, DateTimeOffset.UtcNow.ToString("O"), cancellationToken);

        if (delta > 0 || force)
        {
            _progressHub.Publish(new OperationProgressReport(
                "incremental-scan",
                Application.Progress.OperationCategory.Scan,
                "Incremental Steam scan",
                delta > 0 ? $"Indexed {delta} new game(s)" : "Library up to date",
                IsComplete: true));
        }

        return delta;
    }

    internal async Task<bool> ShouldScanAsync(CancellationToken cancellationToken = default)
    {
        await _settings.InitializeAsync(cancellationToken);
        var last = await _settings.GetAsync(LastScanUtcKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(last))
        {
            return true;
        }

        return !DateTimeOffset.TryParse(last, out var parsed)
            || DateTimeOffset.UtcNow - parsed > ScanThrottle;
    }

    public static int CountNewInstalls(IReadOnlyCollection<int> installed, IReadOnlyCollection<int> knownInstalled)
    {
        if (installed.Count == 0)
        {
            return 0;
        }

        var known = knownInstalled as HashSet<int> ?? knownInstalled.ToHashSet();
        var delta = 0;
        foreach (var appId in installed)
        {
            if (!known.Contains(appId))
            {
                delta++;
            }
        }

        return delta;
    }
}
