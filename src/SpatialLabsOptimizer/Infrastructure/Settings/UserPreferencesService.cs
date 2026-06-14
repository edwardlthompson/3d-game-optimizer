using SpatialLabsOptimizer.Infrastructure.Data;

namespace SpatialLabsOptimizer.Infrastructure.Settings;

public sealed class UserPreferencesService
{
    private readonly SqliteSettingsStore _settings;

    public UserPreferencesService(SqliteSettingsStore settings)
    {
        _settings = settings;
    }

    public async Task<bool> GetSimpleModeAsync(CancellationToken cancellationToken = default)
    {
        var value = await _settings.GetAsync("simple_mode", cancellationToken);
        return value == "true";
    }

    public async Task SetSimpleModeAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        await _settings.SetAsync("simple_mode", enabled ? "true" : "false", cancellationToken);
    }

    public async Task<bool> GetTrainerCoexistenceAsync(CancellationToken cancellationToken = default)
    {
        var value = await _settings.GetAsync("trainer_coexistence", cancellationToken);
        return value != "false";
    }

    public async Task SetTrainerCoexistenceAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        await _settings.SetAsync("trainer_coexistence", enabled ? "true" : "false", cancellationToken);
    }

    public async Task<bool> GetSafeLaunchAsync(CancellationToken cancellationToken = default)
    {
        var value = await _settings.GetAsync("safe_launch", cancellationToken);
        return value == "true";
    }

    public async Task SetSafeLaunchAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        await _settings.SetAsync("safe_launch", enabled ? "true" : "false", cancellationToken);
    }
}
