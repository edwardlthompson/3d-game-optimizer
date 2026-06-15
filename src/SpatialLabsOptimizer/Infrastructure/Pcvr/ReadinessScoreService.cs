using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Install;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Updates;

namespace SpatialLabsOptimizer.Infrastructure.Pcvr;

public sealed record ReadinessScoreResult(int Score, IReadOnlyList<string> Factors);

public sealed class ReadinessScoreService
{
    private readonly JsonDataLoader _loader;
    private readonly ToolInstallDetector _toolDetector;
    private readonly SqliteSettingsStore _settings;
    private readonly PresetCacheService _presets;

    public ReadinessScoreService(
        JsonDataLoader loader,
        ToolInstallDetector toolDetector,
        SqliteSettingsStore settings,
        PresetCacheService presets)
    {
        _loader = loader;
        _toolDetector = toolDetector;
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
        var toolIds = display?.RequiredToolIds is { Count: > 0 } required
            ? required
            : (tools?.Tools ?? []).Select(t => t.Id).ToList();
        var statuses = await _toolDetector.GetStatusesAsync(toolIds, cancellationToken);
        var installedTools = statuses.Count(s => s.IsInstalled);

        score += Math.Min(25, installedTools * 8);
        factors.Add($"Toolchain components detected: {installedTools}/{toolIds.Count}");

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
}
