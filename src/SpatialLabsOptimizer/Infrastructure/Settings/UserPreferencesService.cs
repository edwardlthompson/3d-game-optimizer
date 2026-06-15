using SpatialLabsOptimizer.Infrastructure.Data;

namespace SpatialLabsOptimizer.Infrastructure.Settings;

public sealed partial class UserPreferencesService
{
    internal const string V2ExperimentalKey = "v2_experimental";
    internal const string LibraryUiPrefsKey = "library_ui_prefs";

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

    public async Task<bool> GetV2ExperimentalAsync(CancellationToken cancellationToken = default)
    {
        var value = await _settings.GetAsync(V2ExperimentalKey, cancellationToken);
        return value == "true";
    }

    public async Task SetV2ExperimentalAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        await _settings.SetAsync(V2ExperimentalKey, enabled ? "true" : "false", cancellationToken);
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

    public async Task<bool> GetModManagerCoexistenceAsync(CancellationToken cancellationToken = default)
    {
        var value = await _settings.GetAsync("mod_manager_coexistence", cancellationToken);
        return value != "false";
    }

    public async Task SetModManagerCoexistenceAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        await _settings.SetAsync("mod_manager_coexistence", enabled ? "true" : "false", cancellationToken);
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

    public async Task<string> GetThemeAsync(CancellationToken cancellationToken = default)
    {
        var value = await _settings.GetAsync("theme", cancellationToken);
        return string.IsNullOrWhiteSpace(value) ? "system" : value;
    }

    public async Task SetThemeAsync(string theme, CancellationToken cancellationToken = default)
    {
        await _settings.SetAsync("theme", theme, cancellationToken);
    }
}
