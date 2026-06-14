using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Launch.Coexistence;
using SpatialLabsOptimizer.Infrastructure.Privacy;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Settings;
using SpatialLabsOptimizer.Infrastructure.Updates;

namespace SpatialLabsOptimizer.Infrastructure.Pcvr;

internal static partial class PathRedactor
{
    public static string Redact(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        var redacted = value;
        var user = Environment.UserName;
        if (!string.IsNullOrWhiteSpace(user))
        {
            redacted = redacted.Replace(user, "REDACTED_USER", StringComparison.OrdinalIgnoreCase);
        }

        return UsersPathRegex().Replace(redacted, @"Users\REDACTED_USER");
    }

    [GeneratedRegex(@"Users\\[^\\]+", RegexOptions.IgnoreCase)]
    private static partial Regex UsersPathRegex();
}

public sealed record LaunchDryRunResult(
    bool WouldSucceed,
    string? PredictedErrorCode,
    IReadOnlyList<string> Steps,
    LaunchPreviewSummary? Preview);

public sealed class LaunchDryRunService
{
    private readonly ResolveGameSettings _resolve;
    private readonly DisplayAutoDetector _detector;
    private readonly PresetCacheService _presetCache;
    private readonly ExternalToolCoexistenceService _coexistence;
    private readonly UserPreferencesService _preferences;
    private readonly LaunchPreviewService _launchPreview;
    private readonly LaunchErrorCatalog _errors;
    private readonly OperationProgressHub _progressHub;

    public LaunchDryRunService(
        ResolveGameSettings resolve,
        DisplayAutoDetector detector,
        PresetCacheService presetCache,
        ExternalToolCoexistenceService coexistence,
        UserPreferencesService preferences,
        LaunchPreviewService launchPreview,
        LaunchErrorCatalog errors,
        OperationProgressHub progressHub)
    {
        _resolve = resolve;
        _detector = detector;
        _presetCache = presetCache;
        _coexistence = coexistence;
        _preferences = preferences;
        _launchPreview = launchPreview;
        _errors = errors;
        _progressHub = progressHub;
    }

    public async Task<LaunchDryRunResult> SimulateAsync(int appId, CancellationToken cancellationToken = default)
    {
        var steps = new List<string>();
        var profile = await _detector.DetectAsync(cancellationToken);
        if (profile is null)
        {
            steps.Add("Display detection failed");
            return new LaunchDryRunResult(false, "3DGO-0002", steps, null);
        }

        var adapter = _detector.CreateAdapter(profile);
        var plan = await _resolve.ResolveAsync(appId, adapter, cancellationToken);
        if (plan.Platform == LaunchPlatform.Blocked)
        {
            steps.Add("Launch blocked by compatibility tier");
            return new LaunchDryRunResult(false, "3DGO-0003", steps, null);
        }

        var preview = _launchPreview.Summarize(plan);
        var progressSteps = new[]
        {
            _launchPreview.ToProgressMessage(preview),
            "Checking launch readiness…",
            "Ensuring preset cached…",
            "Resolving game settings…",
            "Selecting platform…",
            "Checking trainer compatibility…",
            "Checking mod manager compatibility…",
            "Applying 3D configs…",
            "Applying display optimal defaults…",
            "[Dry run] Would start game — launch skipped"
        };

        for (var i = 0; i < progressSteps.Length; i++)
        {
            steps.Add(progressSteps[i]);
            _progressHub.Publish(new OperationProgressReport(
                $"dry-run-{appId}",
                Application.Progress.OperationCategory.Launch,
                "Play in 3D (dry run)",
                progressSteps[i],
                StepIndex: i + 1,
                TotalSteps: progressSteps.Length,
                PercentComplete: (i + 1) * 100.0 / progressSteps.Length));
            await Task.Delay(10, cancellationToken);
        }

        var globalSafeLaunch = await _preferences.GetSafeLaunchAsync(cancellationToken);
        if (globalSafeLaunch || plan.SafeLaunch)
        {
            steps.Add("Safe launch mode — injectors would be skipped");
            return new LaunchDryRunResult(true, null, steps, preview);
        }

        if (string.Equals(plan.PreferredOutput, "Headset", StringComparison.OrdinalIgnoreCase))
        {
            steps.Add("Headset output — would route to Play in VR");
            return new LaunchDryRunResult(true, null, steps, preview);
        }

        var trainerCoexistence = await _preferences.GetTrainerCoexistenceAsync(cancellationToken);
        var modManagerCoexistence = await _preferences.GetModManagerCoexistenceAsync(cancellationToken);
        var (shouldBlock, launchContext) = _coexistence.Evaluate(trainerCoexistence, modManagerCoexistence);
        if (shouldBlock)
        {
            steps.Add(_errors.Get("3DGO-0004").Message);
            return new LaunchDryRunResult(false, "3DGO-0004", steps, preview);
        }

        if (!await _presetCache.HasPresetAsync(appId, cancellationToken))
        {
            steps.Add("Preset not cached — real launch would download/cache preset");
        }

        if (launchContext.IsGameFirst)
        {
            steps.Add($"Game-first policy with tools: {string.Join(", ", launchContext.DetectedTools)}");
        }

        return new LaunchDryRunResult(true, null, steps, preview);
    }
}

public sealed class LanPresetExportService
{
    private readonly JsonDataLoader _loader;
    private readonly PrivacyGuard _privacyGuard;

    public LanPresetExportService(JsonDataLoader loader, PrivacyGuard privacyGuard)
    {
        _loader = loader;
        _privacyGuard = privacyGuard;
    }

    public sealed record ExportEntry(int AppId, string Title);

    public async Task<string> ExportAllowlistedPresetsAsync(
        IReadOnlyList<ExportEntry> games,
        CancellationToken cancellationToken = default)
    {
        var manifest = await _loader.LoadAsync<PresetManifestDocument>(
            "presets/preset-manifest-v1.json",
            cancellationToken);
        var profiles = manifest?.UevrProfiles ?? [];

        var presets = new List<object>();
        foreach (var game in games)
        {
            var profile = profiles.FirstOrDefault(p => p.SteamAppIds.Contains(game.AppId));
            if (profile is null)
            {
                continue;
            }

            var allowlisted = string.IsNullOrWhiteSpace(profile.Url) ||
                (Uri.TryCreate(profile.Url, UriKind.Absolute, out var uri) &&
                 _privacyGuard.IsHostAllowed(uri.Host));

            presets.Add(new
            {
                appId = game.AppId,
                title = game.Title,
                presetId = profile.Id,
                sha256 = profile.Sha256,
                allowlisted,
                urlHost = string.IsNullOrWhiteSpace(profile.Url) || !Uri.TryCreate(profile.Url, UriKind.Absolute, out var parsed)
                    ? ""
                    : parsed.Host
            });
        }

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appData, "3d-game-optimizer", "exports");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"lan-presets-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.json");
        var payload = new
        {
            exportedAt = DateTimeOffset.UtcNow,
            allowlistOnly = true,
            presets
        };
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(payload), cancellationToken);
        return path;
    }

    private sealed class PresetManifestDocument
    {
        public List<PresetProfileEntry>? UevrProfiles { get; set; }
    }

    private sealed class PresetProfileEntry
    {
        public string Id { get; set; } = "";
        public string Url { get; set; } = "";
        public string Sha256 { get; set; } = "";
        public List<int> SteamAppIds { get; set; } = [];
    }
}

public sealed record ReadinessScoreResult(int Score, IReadOnlyList<string> Factors);

public sealed class ReadinessScoreService
{
    private readonly JsonDataLoader _loader;
    private readonly ToolPathResolver _toolPaths;
    private readonly SqliteSettingsStore _settings;
    private readonly PresetCacheService _presets;

    public ReadinessScoreService(
        JsonDataLoader loader,
        ToolPathResolver toolPaths,
        SqliteSettingsStore settings,
        PresetCacheService presets)
    {
        _loader = loader;
        _toolPaths = toolPaths;
        _settings = settings;
        _presets = presets;
    }

    public async Task<ReadinessScoreResult> ComputeAsync(
        DisplayProfile? display,
        bool offlineOnboarding,
        string? muxWarning,
        CancellationToken cancellationToken = default)
    {
        var factors = new List<string>();
        var score = 0;

        if (display is not null)
        {
            score += 25;
            factors.Add($"Display profile: {display.MarketingName}");
        }
        else
        {
            factors.Add("No display profile selected");
        }

        var tools = await _loader.LoadAsync<ToolManifestDocument>("tools/tool-manifest-v1.json", cancellationToken);
        var installedTools = 0;
        foreach (var tool in tools?.Tools ?? [])
        {
            if (IsToolPresent(tool.Id))
            {
                installedTools++;
            }
        }

        score += Math.Min(25, installedTools * 8);
        factors.Add($"Toolchain components detected: {installedTools}");

        var benchmarkRaw = await _settings.GetAsync("benchmark_score", cancellationToken);
        if (double.TryParse(benchmarkRaw, out _))
        {
            score += 15;
            factors.Add("Local benchmark completed");
        }

        if (string.IsNullOrWhiteSpace(muxWarning))
        {
            score += 10;
        }
        else
        {
            factors.Add("Dual-GPU MUX warning present");
        }

        if (offlineOnboarding)
        {
            score += 10;
            factors.Add("Offline onboarding — preset bulk cache skipped");
        }
        else
        {
            score += 15;
            factors.Add("Online onboarding completed");
        }

        var manifest = await _loader.LoadAsync<PresetManifestDocument>(
            "presets/preset-manifest-v1.json",
            cancellationToken);
        var sampleAppId = manifest?.UevrProfiles?.FirstOrDefault()?.SteamAppIds.FirstOrDefault() ?? 0;
        if (sampleAppId > 0 && await _presets.HasPresetAsync(sampleAppId, cancellationToken))
        {
            score += 10;
            factors.Add("Sample preset available locally");
        }

        return new ReadinessScoreResult(Math.Min(100, score), factors);
    }

    private bool IsToolPresent(string toolId) => toolId switch
    {
        "uevr" => _toolPaths.ResolveExecutable("uevr", "UEVRInjector.exe") is not null,
        "reshade" => _toolPaths.ResolveExecutable("reshade", "ReShade.dll", "ReShade64.dll") is not null,
        _ => Directory.Exists(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "3d-game-optimizer",
            "tools",
            toolId))
    };

    private sealed class ToolManifestDocument
    {
        public List<ToolEntry>? Tools { get; set; }
    }

    private sealed class ToolEntry
    {
        public string Id { get; set; } = "";
    }

    private sealed class PresetManifestDocument
    {
        public List<PresetProfileEntry>? UevrProfiles { get; set; }
    }

    private sealed class PresetProfileEntry
    {
        public List<int> SteamAppIds { get; set; } = [];
    }
}

public sealed class SeedContributionExportService
{
    private readonly ReadinessScoreService _readiness;
    private readonly DisplayAutoDetector _detector;
    private readonly ExternalToolCoexistenceService _coexistence;
    private readonly UserPreferencesService _preferences;
    private readonly SqliteSettingsStore _settings;
    private readonly InstallArtifactDetector _artifactDetector;

    public SeedContributionExportService(
        ReadinessScoreService readiness,
        DisplayAutoDetector detector,
        ExternalToolCoexistenceService coexistence,
        UserPreferencesService preferences,
        SqliteSettingsStore settings,
        InstallArtifactDetector artifactDetector)
    {
        _readiness = readiness;
        _detector = detector;
        _coexistence = coexistence;
        _preferences = preferences;
        _settings = settings;
        _artifactDetector = artifactDetector;
    }

    public async Task<string> ExportAsync(CancellationToken cancellationToken = default)
    {
        var display = await _detector.DetectAsync(cancellationToken);
        var offlineOnboarding = string.Equals(
            await _settings.GetAsync("offline_onboarding", cancellationToken),
            "true",
            StringComparison.OrdinalIgnoreCase);
        var score = await _readiness.ComputeAsync(display, offlineOnboarding, null, cancellationToken);
        var trainerCoexistence = await _preferences.GetTrainerCoexistenceAsync(cancellationToken);
        var modManagerCoexistence = await _preferences.GetModManagerCoexistenceAsync(cancellationToken);
        var updateInterval = await _preferences.GetUpdateCheckIntervalAsync(cancellationToken);
        var artifactType = await _preferences.GetInstallArtifactTypeAsync(_artifactDetector, cancellationToken);

        var payload = new
        {
            exportedAt = DateTimeOffset.UtcNow,
            productVersion = ProductVersionReader.ReadCurrentVersion(),
            redacted = true,
            readinessScore = score.Score,
            readinessFactors = score.Factors,
            displayProfile = display is null
                ? null
                : new
                {
                    display.Id,
                    display.Vendor,
                    display.MarketingName,
                    display.Type
                },
            coexistence = new
            {
                trainerCoexistenceEnabled = trainerCoexistence,
                modManagerCoexistenceEnabled = modManagerCoexistence,
                detectedTools = _coexistence.GetAllRunningExternalTools()
            },
            updateSettings = new
            {
                checkInterval = updateInterval.ToString(),
                installArtifactType = artifactType.ToString()
            },
            note = "Redacted seed contribution JSON for GitHub issues — no PII or local paths"
        };

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appData, "3d-game-optimizer", "exports");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"seed-contribution-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.json");
        var json = JsonSerializer.Serialize(payload);
        json = PathRedactor.Redact(json);
        await File.WriteAllTextAsync(path, json, cancellationToken);
        return path;
    }
}

public sealed class ProtocolRegistrationService
{
    private const string ProtocolName = "3dgo";

    public string ExecutablePath { get; }

    public ProtocolRegistrationService()
    {
        ExecutablePath = Environment.ProcessPath
            ?? Path.Combine(AppContext.BaseDirectory, "SpatialLabsOptimizer.exe");
    }

    public bool IsRegistered()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{ProtocolName}");
            return key?.GetValue("URL Protocol") is not null;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public bool Register()
    {
        try
        {
            using var protocolKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ProtocolName}");
            protocolKey.SetValue("", $"URL:{ProtocolName} Protocol");
            protocolKey.SetValue("URL Protocol", "");

            using var defaultIcon = protocolKey.CreateSubKey("DefaultIcon");
            defaultIcon.SetValue("", $"\"{ExecutablePath}\",1");

            using var commandKey = protocolKey.CreateSubKey(@"shell\open\command");
            commandKey.SetValue("", $"\"{ExecutablePath}\" \"%1\"");
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public bool Unregister()
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{ProtocolName}", throwOnMissingSubKey: false);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static bool TryParsePlayUri(string? uri, out int appId)
    {
        appId = 0;
        if (string.IsNullOrWhiteSpace(uri))
        {
            return false;
        }

        const string prefix = "3dgo://play/";
        if (uri.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            var segment = uri[prefix.Length..].TrimEnd('/');
            var queryIndex = segment.IndexOf('?', StringComparison.Ordinal);
            if (queryIndex >= 0)
            {
                segment = segment[..queryIndex];
            }

            return int.TryParse(segment, out appId) && appId > 0;
        }

        if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed))
        {
            return false;
        }

        if (!string.Equals(parsed.Scheme, ProtocolName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.Equals(parsed.Host, "play", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var pathSegment = parsed.AbsolutePath.Trim('/');
        return int.TryParse(pathSegment, out appId) && appId > 0;
    }

    public static string? FindProtocolUriInCommandLine()
    {
        foreach (var arg in Environment.GetCommandLineArgs())
        {
            if (arg.StartsWith($"{ProtocolName}://", StringComparison.OrdinalIgnoreCase))
            {
                return arg;
            }
        }

        return null;
    }
}
