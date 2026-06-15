using SpatialLabsOptimizer.Infrastructure.Settings;

namespace SpatialLabsOptimizer.Infrastructure.Updates;

public sealed class CatalogUpdateScheduler
{
    private readonly CatalogUpdateService _catalog;
    private readonly UserPreferencesService _prefs;

    public CatalogUpdateScheduler(CatalogUpdateService catalog, UserPreferencesService prefs)
    {
        _catalog = catalog;
        _prefs = prefs;
    }

    public CatalogUpdateResult? LastResult { get; private set; }

    public async Task RunIfDueAsync(CancellationToken cancellationToken = default)
    {
        var interval = await _prefs.GetCatalogCheckIntervalAsync(cancellationToken);
        if (interval == UpdateCheckInterval.Off)
        {
            return;
        }

        if (!await _catalog.IsCheckDueAsync(cancellationToken))
        {
            return;
        }

        LastResult = await _catalog.CheckForUpdateAsync(cancellationToken: cancellationToken);
    }

    public Task<CatalogUpdateResult> CheckNowAsync(CancellationToken cancellationToken = default)
    {
        return RunCheckNowAsync(cancellationToken);
    }

    private async Task<CatalogUpdateResult> RunCheckNowAsync(CancellationToken cancellationToken)
    {
        LastResult = await _catalog.CheckForUpdateAsync(force: true, cancellationToken: cancellationToken);
        return LastResult;
    }
}
