using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Launch.Coexistence;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Settings;
using SpatialLabsOptimizer.Infrastructure.Updates;

namespace SpatialLabsOptimizer.Infrastructure.Pcvr;

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
