using SpatialLabsOptimizer.Infrastructure.Data;
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
        var installed = _scanner.ScanInstalledAppIds().ToHashSet();
        var knownInstalled = await _database.GetInstalledSteamAppIdsAsync(cancellationToken);
        var delta = CountNewInstalls(installed, knownInstalled);

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
