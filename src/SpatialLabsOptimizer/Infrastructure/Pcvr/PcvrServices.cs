using System.Diagnostics;
using System.Text.Json;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Launch.Coexistence;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Settings;
using SpatialLabsOptimizer.Infrastructure.Updates;

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
        var steamVr = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Steam", "steamapps", "common", "SteamVR");

        if (Directory.Exists(steamVr))
        {
            return "SteamVR";
        }

        var overrideId = _prefs is null
            ? null
            : await _prefs.GetOpenXrRuntimeOverrideAsync(cancellationToken);
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

    public Task<bool> LaunchViaOpenXrAsync(int steamAppId, string? launchOptions = null, CancellationToken cancellationToken = default)
    {
        var runtimePath = OpenXrRuntimeProbe.TryGetActiveRuntimeJsonPath()
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "OpenXR", "runtime.json");
        if (!File.Exists(runtimePath))
        {
            return LaunchViaSteamVrAsync(steamAppId, launchOptions, cancellationToken);
        }

        return LaunchViaSteamVrAsync(steamAppId, launchOptions, cancellationToken);
    }
}

public sealed class DiagnosticBundleService
{
    private readonly LaunchAuditService _audit;
    private readonly DisplayAutoDetector _detector;
    private readonly JsonDataLoader _loader;
    private readonly ToolPathResolver _toolPaths;
    private readonly ExternalToolCoexistenceService _coexistence;
    private readonly UserPreferencesService _preferences;
    private readonly InstallArtifactDetector _artifactDetector;

    public DiagnosticBundleService(
        LaunchAuditService audit,
        DisplayAutoDetector detector,
        JsonDataLoader loader,
        ToolPathResolver toolPaths,
        ExternalToolCoexistenceService coexistence,
        UserPreferencesService preferences,
        InstallArtifactDetector artifactDetector)
    {
        _audit = audit;
        _detector = detector;
        _loader = loader;
        _toolPaths = toolPaths;
        _coexistence = coexistence;
        _preferences = preferences;
        _artifactDetector = artifactDetector;
    }

    public async Task<string> ExportAsync(CancellationToken cancellationToken = default)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var logDir = Path.Combine(appData, "3d-game-optimizer", "logs");
        var bundleDir = Path.Combine(appData, "3d-game-optimizer", "diagnostics");
        Directory.CreateDirectory(bundleDir);
        var stamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");
        var zipPath = Path.Combine(bundleDir, $"bundle-{stamp}.zip");

        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }

        var staging = Path.Combine(bundleDir, $"staging-{stamp}");
        Directory.CreateDirectory(staging);

        if (Directory.Exists(logDir))
        {
            foreach (var log in Directory.EnumerateFiles(logDir, "*.log").Take(5))
            {
                File.Copy(log, Path.Combine(staging, Path.GetFileName(log)), overwrite: true);
            }
        }

        if (File.Exists(_audit.LogPath))
        {
            var auditCopy = Path.Combine(staging, "launch-audit.log");
            File.Copy(_audit.LogPath, auditCopy, overwrite: true);
            var redactedAudit = PathRedactor.Redact(await File.ReadAllTextAsync(auditCopy, cancellationToken));
            await File.WriteAllTextAsync(auditCopy, redactedAudit, cancellationToken);
        }

        var display = await _detector.DetectAsync(cancellationToken);
        await WriteJsonAsync(staging, "display-profile.json", display is null
            ? new { detected = false }
            : new
            {
                display.Id,
                display.Vendor,
                display.MarketingName,
                display.Type,
                display.RecommendedProfileId
            }, cancellationToken);

        var tools = await _loader.LoadAsync<ToolManifestDocument>("tools/tool-manifest-v1.json", cancellationToken);
        var toolchain = (tools?.Tools ?? []).Select(tool => new
        {
            id = tool.Id,
            name = tool.Name,
            installed = IsToolInstalled(tool.Id)
        }).ToList();
        await WriteJsonAsync(staging, "toolchain-versions.json", toolchain, cancellationToken);

        var trainerCoexistence = await _preferences.GetTrainerCoexistenceAsync(cancellationToken);
        var modManagerCoexistence = await _preferences.GetModManagerCoexistenceAsync(cancellationToken);
        await WriteJsonAsync(staging, "coexistence-state.json", new
        {
            trainerCoexistenceEnabled = trainerCoexistence,
            modManagerCoexistenceEnabled = modManagerCoexistence,
            detectedTools = _coexistence.GetAllRunningExternalTools(),
            evaluation = _coexistence.Evaluate(trainerCoexistence, modManagerCoexistence).Context.Policy.ToString()
        }, cancellationToken);

        var updateInterval = await _preferences.GetUpdateCheckIntervalAsync(cancellationToken);
        var artifactType = await _preferences.GetInstallArtifactTypeAsync(_artifactDetector, cancellationToken);
        var lastCheck = await _preferences.GetLastUpdateCheckUtcAsync(cancellationToken);
        await WriteJsonAsync(staging, "update-settings.json", new
        {
            productVersion = ProductVersionReader.ReadCurrentVersion(),
            checkInterval = updateInterval.ToString(),
            installArtifactType = artifactType.ToString(),
            lastUpdateCheckUtc = lastCheck?.ToString("O") ?? "",
            restartPending = await _preferences.GetUpdateRestartPendingAsync(cancellationToken)
        }, cancellationToken);

        await File.WriteAllTextAsync(
            Path.Combine(staging, "manifest.txt"),
            $"Exported {DateTimeOffset.UtcNow:O} — redacted, no PII",
            cancellationToken);

        System.IO.Compression.ZipFile.CreateFromDirectory(staging, zipPath);
        Directory.Delete(staging, recursive: true);
        return zipPath;
    }

    private bool IsToolInstalled(string toolId) => toolId switch
    {
        "uevr" => _toolPaths.ResolveExecutable("uevr", "UEVRInjector.exe") is not null,
        "reshade" => _toolPaths.ResolveExecutable("reshade", "ReShade.dll", "ReShade64.dll") is not null,
        _ => Directory.Exists(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "3d-game-optimizer",
            "tools",
            toolId))
    };

    private static async Task WriteJsonAsync(string dir, string fileName, object payload, CancellationToken cancellationToken)
    {
        var json = PathRedactor.Redact(JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
        await File.WriteAllTextAsync(Path.Combine(dir, fileName), json, cancellationToken);
    }

    private sealed class ToolManifestDocument
    {
        public List<ToolEntry>? Tools { get; set; }
    }

    private sealed class ToolEntry
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
    }
}

public sealed class CommandPaletteService
{
    private readonly List<CommandPaletteEntry> _commands = new()
    {
        new("play-3d", "Play in 3D", "Launch selected game in 3D"),
        new("play-vr", "Play in VR", "Launch selected game in VR"),
        new("setup-wizard", "Run Setup Wizard", "Silent toolchain install"),
        new("refresh-metadata", "Refresh Metadata", "Update Steam store data"),
        new("rescan-library", "Re-scan Library", "Re-index installed games"),
        new("cache-presets", "Cache Top Presets", "Bulk download UEVR presets"),
        new("open-logs", "Open Logs Folder", "Reveal local log directory"),
        new("toggle-safe-launch", "Toggle Safe Launch", "Enable or disable injector-free launches"),
        new("safe-launch", "Safe Launch", "Launch without injectors"),
        new("diagnostic-bundle", "Export Diagnostics", "Create redacted support bundle"),
        new("command-palette", "Command Palette", "Open quick command search")
    };

    public IReadOnlyList<CommandPaletteEntry> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return _commands;
        }

        return _commands
            .Where(c => c.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        c.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}

public sealed record CommandPaletteEntry(string Id, string Title, string Description);
