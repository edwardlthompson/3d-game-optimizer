using System.Text.Json;

using SpatialLabsOptimizer.Infrastructure.Data;

using SpatialLabsOptimizer.Infrastructure.Displays;

using SpatialLabsOptimizer.Infrastructure.Pcvr;

using SpatialLabsOptimizer.Infrastructure.Updates;



namespace SpatialLabsOptimizer.Infrastructure.Settings;



public sealed class UserPreferencesService

{

    internal const string V2ExperimentalKey = "v2_experimental";



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



    public async Task<UpdateCheckInterval> GetUpdateCheckIntervalAsync(CancellationToken cancellationToken = default)

    {

        var value = await _settings.GetAsync("update_check_interval", cancellationToken);

        return Enum.TryParse<UpdateCheckInterval>(value, true, out var interval)

            ? interval

            : UpdateCheckInterval.Weekly;

    }



    public async Task SetUpdateCheckIntervalAsync(UpdateCheckInterval interval, CancellationToken cancellationToken = default)

    {

        await _settings.SetAsync("update_check_interval", interval.ToString(), cancellationToken);

    }



    public async Task<DateTimeOffset?> GetLastUpdateCheckUtcAsync(CancellationToken cancellationToken = default)

    {

        var value = await _settings.GetAsync("last_update_check_utc", cancellationToken);

        return DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;

    }



    public async Task SetLastUpdateCheckUtcAsync(DateTimeOffset timestamp, CancellationToken cancellationToken = default)

    {

        await _settings.SetAsync("last_update_check_utc", timestamp.ToString("O"), cancellationToken);

    }



    public async Task<UpdateCheckResult?> GetCachedUpdateResultAsync(CancellationToken cancellationToken = default)

    {

        var value = await _settings.GetAsync("cached_update_result", cancellationToken);

        if (string.IsNullOrWhiteSpace(value))

        {

            return null;

        }



        return JsonSerializer.Deserialize<UpdateCheckResult>(value);

    }



    public async Task SetCachedUpdateResultAsync(UpdateCheckResult result, CancellationToken cancellationToken = default)

    {

        await _settings.SetAsync("cached_update_result", JsonSerializer.Serialize(result), cancellationToken);

    }



    public async Task<InstallArtifactType> GetInstallArtifactTypeAsync(

        InstallArtifactDetector detector,

        CancellationToken cancellationToken = default)

    {

        var value = await _settings.GetAsync("install_artifact_type", cancellationToken);

        if (Enum.TryParse<InstallArtifactType>(value, true, out var stored))

        {

            return stored;

        }



        var detected = detector.Detect();

        await _settings.SetAsync("install_artifact_type", detected.ToString(), cancellationToken);

        return detected;

    }



    public async Task SetInstallArtifactTypeAsync(InstallArtifactType type, CancellationToken cancellationToken = default)

    {

        await _settings.SetAsync("install_artifact_type", type.ToString(), cancellationToken);

    }



    public async Task<bool> GetUpdateRestartPendingAsync(CancellationToken cancellationToken = default)

    {

        var value = await _settings.GetAsync("update_restart_pending", cancellationToken);

        return value == "true";

    }



    public async Task SetUpdateRestartPendingAsync(bool pending, CancellationToken cancellationToken = default)

    {

        await _settings.SetAsync("update_restart_pending", pending ? "true" : "false", cancellationToken);

    }



    public async Task<string?> GetUpdateAppliedVersionAsync(CancellationToken cancellationToken = default)

    {

        return await _settings.GetAsync("update_applied_version", cancellationToken);

    }



    public async Task SetUpdateAppliedVersionAsync(string version, CancellationToken cancellationToken = default)

    {

        await _settings.SetAsync("update_applied_version", version, cancellationToken);

    }



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

}

