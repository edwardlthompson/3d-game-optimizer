using System.Diagnostics;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Settings;

namespace SpatialLabsOptimizer.Infrastructure.Pcvr;

public sealed class PcvrRuntimeConnector
{
    private readonly UserPreferencesService? _prefs;

    public PcvrRuntimeConnector(UserPreferencesService? prefs = null)
    {
        _prefs = prefs;
    }

    public async Task<string?> DetectRuntimeAsync(CancellationToken cancellationToken = default)
    {
        var overrideId = _prefs is null
            ? null
            : await _prefs.GetOpenXrRuntimeOverrideAsync(cancellationToken);

        if (string.Equals(overrideId, "off", StringComparison.OrdinalIgnoreCase))
        {
            await Task.CompletedTask;
            return null;
        }

        var steamVr = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Steam", "steamapps", "common", "SteamVR");

        if (Directory.Exists(steamVr))
        {
            return "SteamVR";
        }

        var activeOpenXr = OpenXrRuntimeProbe.TryResolveActiveRuntimeLabel(overrideId);
        if (activeOpenXr is not null)
        {
            return activeOpenXr;
        }

        var openXr = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "OpenXR");

        if (Directory.Exists(openXr))
        {
            return "OpenXR";
        }

        await Task.CompletedTask;
        return null;
    }

    public bool IsRuntimeAvailable() =>
        DetectRuntimeAsync().GetAwaiter().GetResult() is not null;

    public Task<bool> LaunchViaSteamVrAsync(int steamAppId, string? launchOptions = null, CancellationToken cancellationToken = default)
    {
        var steamExe = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Steam", "steam.exe");
        if (!File.Exists(steamExe))
        {
            return Task.FromResult(false);
        }

        var arguments = $"-applaunch {steamAppId}";
        if (!string.IsNullOrWhiteSpace(launchOptions))
        {
            arguments += " " + launchOptions.Trim();
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = steamExe,
                Arguments = arguments,
                UseShellExecute = false
            });
            return Task.FromResult(true);
        }
        catch (Exception)
        {
            return Task.FromResult(false);
        }
    }

    public async Task<bool> LaunchViaOpenXrAsync(int steamAppId, string? launchOptions = null, CancellationToken cancellationToken = default)
    {
        if (_prefs is not null &&
            string.Equals(await _prefs.GetOpenXrRuntimeOverrideAsync(cancellationToken), "off", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var runtimePath = OpenXrRuntimeProbe.TryGetActiveRuntimeJsonPath();
        if (runtimePath is not null)
        {
            var steamVrRoot = OpenXrRuntimeProbe.TryResolveSteamVrRootFromRuntimeJson(runtimePath);
            if (steamVrRoot is not null)
            {
                var vrStartup = Path.Combine(steamVrRoot, "bin", "win64", "vrstartup.exe");
                if (!File.Exists(vrStartup))
                {
                    vrStartup = Path.Combine(steamVrRoot, "vrstartup.exe");
                }

                if (File.Exists(vrStartup))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = vrStartup,
                            UseShellExecute = false
                        });
                        await Task.Delay(750, cancellationToken);
                    }
                    catch (Exception)
                    {
                        // Fall through to Steam applaunch.
                    }
                }
            }
        }

        var vrOptions = string.IsNullOrWhiteSpace(launchOptions)
            ? "-mode vr"
            : $"{launchOptions.Trim()} -mode vr";
        return await LaunchViaSteamVrAsync(steamAppId, vrOptions, cancellationToken);
    }
}
