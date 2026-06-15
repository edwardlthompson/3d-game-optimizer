#!/usr/bin/env python3
"""Split oversized files for Sprint 44 modularization."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / "src" / "SpatialLabsOptimizer"


def write(rel: str, content: str) -> None:
    path = ROOT / rel
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content.rstrip() + "\n", encoding="utf-8")
    print(f"wrote {rel}")


def slice_file(source: Path, start: int, end: int) -> str:
    lines = source.read_text(encoding="utf-8").splitlines()
    return "\n".join(lines[start - 1 : end])


def split_future_services() -> None:
    source = ROOT / "Infrastructure/Updates/FutureServices.cs"
    blocks = {
        "Infrastructure/Updates/IncrementalSteamScanService.cs": (
            13,
            73,
            """using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Library;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Steam;

namespace SpatialLabsOptimizer.Infrastructure.Updates;
""",
        ),
        "Infrastructure/Updates/HdrWatchdogService.cs": (
            75,
            152,
            """using Microsoft.Win32;

namespace SpatialLabsOptimizer.Infrastructure.Updates;
""",
        ),
        "Infrastructure/Updates/PlayQueueService.cs": (
            154,
            165,
            "namespace SpatialLabsOptimizer.Infrastructure.Updates;\n",
        ),
        "Infrastructure/Updates/SessionProfileService.cs": (
            167,
            231,
            """using System.Text.Json;
using SpatialLabsOptimizer.Infrastructure.Data;

namespace SpatialLabsOptimizer.Infrastructure.Updates;
""",
        ),
        "Infrastructure/Updates/SteamGridDbClient.cs": (
            233,
            277,
            """using System.Text.Json;
using SpatialLabsOptimizer.Infrastructure.Artwork;
using SpatialLabsOptimizer.Infrastructure.Data;

namespace SpatialLabsOptimizer.Infrastructure.Updates;
""",
        ),
        "Infrastructure/Updates/LanPartyExportService.cs": (
            279,
            304,
            """using System.Text.Json;

namespace SpatialLabsOptimizer.Infrastructure.Updates;
""",
        ),
        "Infrastructure/Updates/HybridSessionService.cs": (
            307,
            358,
            """using System.Text.Json;
using SpatialLabsOptimizer.Infrastructure.Data;

namespace SpatialLabsOptimizer.Infrastructure.Updates;
""",
        ),
        "Infrastructure/Updates/ModManagerIntegrationService.cs": (
            360,
            370,
            """using SpatialLabsOptimizer.Infrastructure.Launch.Coexistence;

namespace SpatialLabsOptimizer.Infrastructure.Updates;
""",
        ),
        "Infrastructure/Updates/WorkshopPresetImporter.cs": (
            372,
            477,
            """using System.Text.Json;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Launch;

namespace SpatialLabsOptimizer.Infrastructure.Updates;
""",
        ),
    }
    for rel, (start, end, header) in blocks.items():
        write(rel, header + "\n" + slice_file(source, start, end))
    source.unlink()


def split_use_cases() -> None:
    source = ROOT / "Application/UseCases/UseCases.cs"
    header = """using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Compatibility;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Install;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Launch.Coexistence;
using SpatialLabsOptimizer.Infrastructure.Pcvr;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Library;
using SpatialLabsOptimizer.Infrastructure.Settings;

namespace SpatialLabsOptimizer.Application.UseCases;
"""
    blocks = {
        "Application/UseCases/RunSilentSetup.cs": (15, 52),
        "Application/UseCases/PlayIn3D.cs": (54, 330),
        "Application/UseCases/PlayInVR.cs": (332, 400),
        "Application/UseCases/ApplyOptimalDefaults.cs": (402, 421),
        "Application/UseCases/ValidateLaunch.cs": (423, 449),
    }
    for rel, (start, end) in blocks.items():
        write(rel, header + "\n" + slice_file(source, start, end))
    source.unlink()


def split_launch_services() -> None:
    source = ROOT / "Infrastructure/Launch/LaunchServices.cs"
    blocks = {
        "Infrastructure/Launch/LaunchReadinessService.cs": (
            11,
            39,
            """using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Progress;

namespace SpatialLabsOptimizer.Infrastructure.Launch;
""",
        ),
        "Infrastructure/Launch/PresetCacheService.cs": (
            41,
            166,
            """using System.Text.Json;
using SpatialLabsOptimizer.Infrastructure.Compatibility;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Progress;

namespace SpatialLabsOptimizer.Infrastructure.Launch;
""",
        ),
        "Infrastructure/Launch/LaunchPlatformRouter.cs": (
            168,
            187,
            """using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Displays;

namespace SpatialLabsOptimizer.Infrastructure.Launch;
""",
        ),
        "Infrastructure/Launch/ResolveGameSettings.cs": (
            189,
            236,
            """using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Compatibility;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Install;

namespace SpatialLabsOptimizer.Infrastructure.Launch;
""",
        ),
        "Infrastructure/Launch/GameOverrideRepository.cs": (
            238,
            286,
            """using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Data;

namespace SpatialLabsOptimizer.Infrastructure.Launch;
""",
        ),
        "Infrastructure/Launch/LaunchErrorCatalog.cs": (
            288,
            301,
            "namespace SpatialLabsOptimizer.Infrastructure.Launch;\n",
        ),
        "Infrastructure/Launch/SafeLaunchService.cs": (
            303,
            314,
            """using SpatialLabsOptimizer.Infrastructure.Install;

namespace SpatialLabsOptimizer.Infrastructure.Launch;
""",
        ),
    }
    for rel, (start, end, header) in blocks.items():
        write(rel, header + "\n" + slice_file(source, start, end))
    source.unlink()


def split_game_database() -> None:
    source = ROOT / "Infrastructure/Data/GameDatabase.cs"
    original_lines = source.read_text(encoding="utf-8").splitlines()
    partial_header = """using Microsoft.Data.Sqlite;
using SpatialLabsOptimizer.Domain;

namespace SpatialLabsOptimizer.Infrastructure.Data;

public sealed partial class GameDatabase
"""
    write(
        "Infrastructure/Data/GameDatabase.cs",
        partial_header
        + "{\n"
        + "\n".join(original_lines[7:79])
        + "\n\n"
        + "\n".join(original_lines[349:365])
        + "\n}",
    )
    write(
        "Infrastructure/Data/GameDatabase.LocalInstalls.cs",
        partial_header + "{\n" + "\n".join(original_lines[80:178]) + "\n}",
    )
    write(
        "Infrastructure/Data/GameDatabase.RecentLaunches.cs",
        partial_header + "{\n" + "\n".join(original_lines[179:223]) + "\n}",
    )
    write(
        "Infrastructure/Data/GameDatabase.Games.cs",
        partial_header
        + "{\n"
        + "\n".join(original_lines[224:348])
        + "\n}",
    )
    write(
        "Infrastructure/Data/GameDatabase.Records.cs",
        "namespace SpatialLabsOptimizer.Infrastructure.Data;\n\n"
        + "\n".join(original_lines[367:381]),
    )


def split_user_preferences() -> None:
    write(
        "Infrastructure/Settings/UserPreferencesService.cs",
        """using SpatialLabsOptimizer.Infrastructure.Data;

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
""",
    )
    write(
        "Infrastructure/Settings/UserPreferencesService.Updates.cs",
        """using System.Text.Json;
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
}
""",
    )
    write(
        "Infrastructure/Settings/UserPreferencesService.Display.cs",
        """using System.Text.Json;
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
""",
    )


def extract_command_palette() -> None:
    source = ROOT / "Infrastructure/Pcvr/PcvrServices.cs"
    lines = source.read_text(encoding="utf-8").splitlines()
    write(
        "Infrastructure/Pcvr/CommandPaletteService.cs",
        "namespace SpatialLabsOptimizer.Infrastructure.Pcvr;\n\n" + "\n".join(lines[268:300]),
    )
    source.write_text("\n".join(lines[:268]).rstrip() + "\n", encoding="utf-8")


def split_global_settings_view() -> None:
    source = ROOT / "Views/Global3DSettingsView.xaml.cs"
    view_header = """using Microsoft.Extensions.DependencyInjection;
using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Launch.Coexistence;
using SpatialLabsOptimizer.Infrastructure.Performance;
using SpatialLabsOptimizer.Infrastructure.Pcvr;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Settings;
using SpatialLabsOptimizer.Infrastructure.Updates;
using SpatialLabsOptimizer.ViewModels;

namespace SpatialLabsOptimizer.Views;
"""
    write(
        "Views/Global3DSettingsView.xaml.cs",
        view_header
        + """
public sealed partial class Global3DSettingsView : Microsoft.UI.Xaml.Controls.Page
"""
        + "{\n"
        + slice_file(source, 18, 78)
        + "\n\n"
        + slice_file(source, 119, 153)
        + "\n\n"
        + slice_file(source, 201, 216)
        + "\n\n"
        + slice_file(source, 311, 383)
        + "\n\n"
        + slice_file(source, 418, 441)
        + "\n}",
    )
    write(
        "Views/Global3DSettingsView.Snapshots.cs",
        view_header
        + """
public sealed partial class Global3DSettingsView
"""
        + "{\n"
        + slice_file(source, 80, 117)
        + "\n}",
    )
    write(
        "Views/Global3DSettingsView.DisplayLaunch.cs",
        view_header
        + """
public sealed partial class Global3DSettingsView
"""
        + "{\n"
        + slice_file(source, 155, 199)
        + "\n\n"
        + slice_file(source, 385, 416)
        + "\n}",
    )
    write(
        "Views/Global3DSettingsView.SessionProfiles.cs",
        view_header
        + """
public sealed partial class Global3DSettingsView
"""
        + "{\n"
        + slice_file(source, 218, 309)
        + "\n}",
    )


def main() -> None:
    split_future_services()
    split_use_cases()
    split_launch_services()
    split_game_database()
    split_user_preferences()
    extract_command_palette()
    split_global_settings_view()
    print("Sprint 44 splits complete")


if __name__ == "__main__":
    main()
