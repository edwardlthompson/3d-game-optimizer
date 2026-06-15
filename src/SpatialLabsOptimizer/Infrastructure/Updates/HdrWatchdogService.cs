using Microsoft.Win32;

namespace SpatialLabsOptimizer.Infrastructure.Updates;

public sealed class HdrWatchdogService
{
    private const string HdrSettingsKey = @"Software\Microsoft\Windows\CurrentVersion\HDR";
    public const string OsHandoffInstructions =
        "If HDR still looks enabled, open Windows Settings → System → Display → HDR and turn it off before playing in 3D.";

    public HdrDisableMethod LastDisableMethod { get; private set; } = HdrDisableMethod.NotNeeded;

    public string? LastHandoffHint { get; private set; }

    public Task<bool> IsHdrEnabledAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(HdrSettingsKey);
            var value = key?.GetValue("LastKnownEnabledState");
            if (value is int enabled)
            {
                return Task.FromResult(enabled == 1);
            }
        }
        catch (Exception)
        {
            // Registry unavailable — assume SDR.
        }

        return Task.FromResult(false);
    }

    public async Task<bool> DisableHdrFor3DAsync(CancellationToken cancellationToken = default)
    {
        var result = await DisableHdrFor3DWithOutcomeAsync(cancellationToken);
        return result.Method is HdrDisableMethod.Registry or HdrDisableMethod.OsHandoffFlag;
    }

    public async Task<HdrDisableResult> DisableHdrFor3DWithOutcomeAsync(CancellationToken cancellationToken = default)
    {
        LastDisableMethod = HdrDisableMethod.NotNeeded;
        LastHandoffHint = null;

        if (!await IsHdrEnabledAsync(cancellationToken))
        {
            return new HdrDisableResult(HdrDisableMethod.NotNeeded, null);
        }

        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(HdrSettingsKey, true);
            key?.SetValue("LastKnownEnabledState", 0, RegistryValueKind.DWord);
            LastDisableMethod = HdrDisableMethod.Registry;
            return new HdrDisableResult(HdrDisableMethod.Registry, null);
        }
        catch (Exception)
        {
            var flagPath = GetHandoffFlagPath();
            Directory.CreateDirectory(Path.GetDirectoryName(flagPath)!);
            await File.WriteAllTextAsync(flagPath, DateTimeOffset.UtcNow.ToString("O"), cancellationToken);
            LastDisableMethod = HdrDisableMethod.OsHandoffFlag;
            LastHandoffHint = OsHandoffInstructions;
            return new HdrDisableResult(HdrDisableMethod.OsHandoffFlag, OsHandoffInstructions);
        }
    }

    public static string GetHandoffFlagPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "3d-game-optimizer", "config", "hdr-disable-requested.flag");
    }
}

public enum HdrDisableMethod
{
    NotNeeded,
    Registry,
    OsHandoffFlag
}

public sealed record HdrDisableResult(HdrDisableMethod Method, string? HandoffHint);
