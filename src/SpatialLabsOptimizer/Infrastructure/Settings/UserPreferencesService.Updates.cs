using System.Text.Json;
using SpatialLabsOptimizer.Infrastructure.Updates;

namespace SpatialLabsOptimizer.Infrastructure.Settings;

public sealed partial class UserPreferencesService
{
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
        if (string.Equals(value, "Msix", StringComparison.OrdinalIgnoreCase))
        {
            value = null;
        }

        if (!string.IsNullOrWhiteSpace(value) &&
            Enum.TryParse<InstallArtifactType>(value, true, out var stored))
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
}
