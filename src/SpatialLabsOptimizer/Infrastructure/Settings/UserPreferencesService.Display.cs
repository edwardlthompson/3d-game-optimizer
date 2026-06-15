using System.Text.Json;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Pcvr;

namespace SpatialLabsOptimizer.Infrastructure.Settings;

public sealed partial class UserPreferencesService
{
    public async Task<string?> GetLaunchTargetDisplayAsync(CancellationToken cancellationToken = default)
    {
        return await _settings.GetAsync(MultiMonitorLaunchPicker.PreferenceKey, cancellationToken);
    }

    public async Task SetLaunchTargetDisplayAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        await _settings.SetAsync(MultiMonitorLaunchPicker.PreferenceKey, deviceId, cancellationToken);
    }

    public async Task<string?> GetOpenXrRuntimeOverrideAsync(CancellationToken cancellationToken = default)
    {
        return await _settings.GetAsync(OpenXrRuntimePicker.PreferenceKey, cancellationToken);
    }

    public async Task SetOpenXrRuntimeOverrideAsync(string overrideId, CancellationToken cancellationToken = default)
    {
        await _settings.SetAsync(OpenXrRuntimePicker.PreferenceKey, overrideId, cancellationToken);
    }

    public async Task<LibraryUiPrefs> GetLibraryUiPrefsAsync(CancellationToken cancellationToken = default)
    {
        var value = await _settings.GetAsync(LibraryUiPrefsKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(value))
        {
            return new LibraryUiPrefs();
        }

        return JsonSerializer.Deserialize<LibraryUiPrefs>(value) ?? new LibraryUiPrefs();
    }

    public async Task SetLibraryUiPrefsAsync(LibraryUiPrefs prefs, CancellationToken cancellationToken = default)
    {
        await _settings.SetAsync(LibraryUiPrefsKey, JsonSerializer.Serialize(prefs), cancellationToken);
    }
}
