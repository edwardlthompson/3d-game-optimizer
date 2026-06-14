using SpatialLabsOptimizer.Infrastructure.Settings;

namespace SpatialLabsOptimizer.Infrastructure.Updates;

public sealed class UpdateScheduler
{
    private readonly UpdateService _updates;
    private readonly UserPreferencesService _prefs;

    public UpdateScheduler(UpdateService updates, UserPreferencesService prefs)
    {
        _updates = updates;
        _prefs = prefs;
    }

    public UpdateCheckResult? LastResult { get; private set; }

    public bool IsUpdateAvailable => LastResult?.IsUpdateAvailable == true;

    public async Task RunIfDueAsync(CancellationToken cancellationToken = default)
    {
        var interval = await _prefs.GetUpdateCheckIntervalAsync(cancellationToken);
        if (interval == UpdateCheckInterval.Off)
        {
            LastResult = await _prefs.GetCachedUpdateResultAsync(cancellationToken);
            return;
        }

        if (!await _updates.IsCheckDueAsync(cancellationToken))
        {
            LastResult = await _prefs.GetCachedUpdateResultAsync(cancellationToken);
            return;
        }

        LastResult = await _updates.CheckForUpdateAsync(cancellationToken: cancellationToken);
    }

    public async Task<UpdateCheckResult> CheckNowAsync(CancellationToken cancellationToken = default)
    {
        LastResult = await _updates.CheckForUpdateAsync(cancellationToken: cancellationToken);
        return LastResult;
    }
}
