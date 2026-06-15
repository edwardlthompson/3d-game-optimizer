using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Performance;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Updates;

namespace SpatialLabsOptimizer.ViewModels;

public sealed partial class Global3DSettingsViewModel
{
    private readonly GameOverrideRepository _overrides;
    private readonly PresetCacheService _presets;
    private readonly OperationProgressHub _progressHub;
    private readonly BenchmarkService _benchmark;
    private readonly HdrWatchdogService? _hdrWatchdog;

    private string _overrideAppId = string.Empty;
    private double _overrideDepth = 0.65;
    private double _overrideConvergence = 0.5;
    private int _overridePlatformIndex;
    private string _overrideStatus = string.Empty;
    private bool _hdrPanelVisible;
    private string _hdrNoticeText = string.Empty;
    private string _bulkPresetStatus = string.Empty;
    private string _benchmarkResult = string.Empty;
    private bool _showViewingDistanceCoach;

    public string OverrideAppId
    {
        get => _overrideAppId;
        set => SetProperty(ref _overrideAppId, value);
    }

    public double OverrideDepth
    {
        get => _overrideDepth;
        set => SetProperty(ref _overrideDepth, value);
    }

    public double OverrideConvergence
    {
        get => _overrideConvergence;
        set => SetProperty(ref _overrideConvergence, value);
    }

    public int OverridePlatformIndex
    {
        get => _overridePlatformIndex;
        set => SetProperty(ref _overridePlatformIndex, value);
    }

    public string OverrideStatus
    {
        get => _overrideStatus;
        private set => SetProperty(ref _overrideStatus, value);
    }

    public bool HdrPanelVisible
    {
        get => _hdrPanelVisible;
        private set => SetProperty(ref _hdrPanelVisible, value);
    }

    public string HdrNoticeText
    {
        get => _hdrNoticeText;
        private set => SetProperty(ref _hdrNoticeText, value);
    }

    public string BulkPresetStatus
    {
        get => _bulkPresetStatus;
        private set => SetProperty(ref _bulkPresetStatus, value);
    }

    public string BenchmarkResult
    {
        get => _benchmarkResult;
        private set => SetProperty(ref _benchmarkResult, value);
    }

    public bool ShowViewingDistanceCoach
    {
        get => _showViewingDistanceCoach;
        set => SetProperty(ref _showViewingDistanceCoach, value);
    }

    private void ToggleViewingDistanceCoach() => ShowViewingDistanceCoach = !ShowViewingDistanceCoach;

    private async Task SaveOverrideAsync()
    {
        if (!int.TryParse(OverrideAppId.Trim(), out var appId) || appId <= 0)
        {
            OverrideStatus = "Enter a valid Steam App ID.";
            return;
        }

        LaunchPlatform? platform = OverridePlatformIndex switch
        {
            1 => LaunchPlatform.Uevr,
            2 => LaunchPlatform.ReShade,
            3 => LaunchPlatform.TrueGame,
            _ => null
        };

        await _overrides.SaveAsync(new GameOverride(
            appId,
            OverrideDepth,
            OverrideConvergence,
            platform,
            false,
            "Auto"));
        OverrideStatus = $"Saved override for app {appId}.";
    }

    private async Task DisableHdrAsync()
    {
        if (_hdrWatchdog is null)
        {
            return;
        }

        var disabled = await _hdrWatchdog.DisableHdrFor3DAsync();
        HdrNoticeText = disabled
            ? "HDR disable requested. Confirm in Windows display settings if needed."
            : "HDR already disabled or unavailable.";
    }

    private async Task CachePresetsAsync()
    {
        BulkPresetStatus = "Caching presets…";
        await _presets.BulkCacheTopPresetsAsync(50, _progressHub);
        BulkPresetStatus = "Top presets cached.";
    }

    private async Task RunBenchmarkAsync()
    {
        var score = await _benchmark.RunBenchmarkAsync();
        BenchmarkResult = $"Benchmark score: {score:F0}";
    }

    private async Task LoadHdrSectionAsync()
    {
        if (_hdrWatchdog is null)
        {
            return;
        }

        if (await _hdrWatchdog.IsHdrEnabledAsync())
        {
            HdrPanelVisible = true;
            HdrNoticeText =
                "Windows HDR is enabled. 3D sessions may look washed out until HDR is disabled for SDR handoff.";
        }
    }
}
