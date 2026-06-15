using SpatialLabsOptimizer.Infrastructure.Settings;

namespace SpatialLabsOptimizer.Infrastructure.Updates;

public sealed partial class UpdateService
{
    public async Task<bool> IsCheckDueAsync(CancellationToken cancellationToken = default)
    {
        var interval = await _prefs.GetUpdateCheckIntervalAsync(cancellationToken);
        if (interval == UpdateCheckInterval.Off)
        {
            return false;
        }

        if (interval == UpdateCheckInterval.Startup)
        {
            return true;
        }

        var lastCheck = await _prefs.GetLastUpdateCheckUtcAsync(cancellationToken);
        if (lastCheck is null)
        {
            return true;
        }

        var elapsed = DateTimeOffset.UtcNow - lastCheck.Value;
        return interval switch
        {
            UpdateCheckInterval.Daily => elapsed >= TimeSpan.FromHours(24),
            UpdateCheckInterval.Weekly => elapsed >= TimeSpan.FromDays(7),
            _ => false
        };
    }
}
