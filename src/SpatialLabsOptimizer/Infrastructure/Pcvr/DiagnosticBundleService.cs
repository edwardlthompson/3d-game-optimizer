using System.Text.Json;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Install;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Launch.Coexistence;
using SpatialLabsOptimizer.Infrastructure.Settings;
using SpatialLabsOptimizer.Infrastructure.Updates;

namespace SpatialLabsOptimizer.Infrastructure.Pcvr;

public sealed class DiagnosticBundleService
{
    private readonly LaunchAuditService _audit;
    private readonly DisplayAutoDetector _detector;
    private readonly JsonDataLoader _loader;
    private readonly ToolInstallDetector _toolDetector;
    private readonly ExternalToolCoexistenceService _coexistence;
    private readonly UserPreferencesService _preferences;
    private readonly InstallArtifactDetector _artifactDetector;

    public DiagnosticBundleService(
        LaunchAuditService audit,
        DisplayAutoDetector detector,
        JsonDataLoader loader,
        ToolInstallDetector toolDetector,
        ExternalToolCoexistenceService coexistence,
        UserPreferencesService preferences,
        InstallArtifactDetector artifactDetector)
    {
        _audit = audit;
        _detector = detector;
        _loader = loader;
        _toolDetector = toolDetector;
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
        var toolchain = new List<object>();
        foreach (var tool in tools?.Tools ?? [])
        {
            toolchain.Add(new
            {
                id = tool.Id,
                name = tool.Name,
                installed = await _toolDetector.IsInstalledAsync(tool.Id, cancellationToken)
            });
        }
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

    private static async Task WriteJsonAsync(string dir, string fileName, object payload, CancellationToken cancellationToken)
    {
        var json = PathRedactor.Redact(JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
        await File.WriteAllTextAsync(Path.Combine(dir, fileName), json, cancellationToken);
    }
}
