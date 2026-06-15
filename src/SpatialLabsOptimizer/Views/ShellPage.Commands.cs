using System.Diagnostics;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Pcvr;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Settings;
using SpatialLabsOptimizer.ViewModels;

namespace SpatialLabsOptimizer.Views;

public sealed partial class ShellPage
{
    public async Task ExecuteCommandAsync(string commandId)
    {
        switch (commandId)
        {
            case "setup-wizard":
                _settingsViewModel.ExpandToolchain = true;
                NavigateToTag("settings");
                break;
            case "play-3d":
                NavigateToTag("library");
                _libraryViewModel.PlayCommand.Execute(null);
                break;
            case "play-vr":
                NavigateToTag("library");
                _libraryViewModel.PlayVrCommand.Execute(null);
                break;
            case "refresh-metadata":
            case "rescan-library":
                NavigateToTag("library");
                await _libraryViewModel.LoadAsync();
                ViewModel.Status = "Library re-indexed.";
                break;
            case "cache-presets":
                await CacheTopPresetsAsync();
                break;
            case "open-logs":
                OpenLogsFolder();
                break;
            case "toggle-safe-launch":
                await ToggleSafeLaunchAsync();
                break;
            case "safe-launch":
                NavigateToTag("settings");
                break;
            case "diagnostic-bundle":
                NavigateToTag("troubleshoot");
                break;
            case "command-palette":
                NavigateToTag("commands");
                break;
        }
    }

    private async Task CacheTopPresetsAsync()
    {
        ViewModel.Status = "Caching top presets…";
        await _presets.BulkCacheTopPresetsAsync(50, _progressHub);
        ViewModel.Status = "Top presets cached.";
    }

    private static void OpenLogsFolder()
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "3d-game-optimizer", "logs");
        Directory.CreateDirectory(logDir);
        Process.Start(new ProcessStartInfo
        {
            FileName = logDir,
            UseShellExecute = true
        });
    }

    private async Task ToggleSafeLaunchAsync()
    {
        var enabled = await _prefs.GetSafeLaunchAsync();
        await _prefs.SetSafeLaunchAsync(!enabled);
        ViewModel.Status = !enabled ? "Safe launch enabled." : "Safe launch disabled.";
    }
}
