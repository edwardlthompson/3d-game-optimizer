using System.Text.Json;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Launch.Coexistence;
using SpatialLabsOptimizer.Infrastructure.Settings;
using SpatialLabsOptimizer.Infrastructure.Updates;

namespace SpatialLabsOptimizer.Infrastructure.Pcvr;

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
