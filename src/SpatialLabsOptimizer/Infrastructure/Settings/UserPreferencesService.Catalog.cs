using SpatialLabsOptimizer.Infrastructure.Updates;

namespace SpatialLabsOptimizer.Infrastructure.Settings;

public sealed partial class UserPreferencesService
{
    public async Task<UpdateCheckInterval> GetCatalogCheckIntervalAsync(CancellationToken cancellationToken = default)
    {
        var value = await _settings.GetAsync("catalog_check_interval", cancellationToken);
        return Enum.TryParse<UpdateCheckInterval>(value, true, out var interval)
            ? interval
            : UpdateCheckInterval.Off;
    }

    public async Task SetCatalogCheckIntervalAsync(UpdateCheckInterval interval, CancellationToken cancellationToken = default)
    {
        await _settings.SetAsync("catalog_check_interval", interval.ToString(), cancellationToken);
    }

    public async Task<DateTimeOffset?> GetLastCatalogCheckUtcAsync(CancellationToken cancellationToken = default)
    {
        var value = await _settings.GetAsync("last_catalog_check_utc", cancellationToken);
        return DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;
    }

    public async Task SetLastCatalogCheckUtcAsync(DateTimeOffset timestamp, CancellationToken cancellationToken = default)
    {
        await _settings.SetAsync("last_catalog_check_utc", timestamp.ToString("O"), cancellationToken);
    }
}
