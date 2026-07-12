using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Pcvr;

namespace SpatialLabsOptimizer.ViewModels;

public sealed partial class Global3DSettingsViewModel
{
    private ReadinessScoreService? _readinessScore;
    private DisplayAutoDetector? _displayDetector;
    private int _readinessScoreValue;
    private string _readinessFactorsText = "";

    public int ReadinessScoreValue
    {
        get => _readinessScoreValue;
        private set => SetProperty(ref _readinessScoreValue, value);
    }

    public string ReadinessFactorsText
    {
        get => _readinessFactorsText;
        private set => SetProperty(ref _readinessFactorsText, value);
    }

    public void AttachReadiness(ReadinessScoreService readiness, DisplayAutoDetector detector)
    {
        _readinessScore = readiness;
        _displayDetector = detector;
    }

    public async Task RefreshReadinessAsync()
    {
        if (_readinessScore is null || _displayDetector is null)
        {
            return;
        }

        var display = await _displayDetector.DetectAsync();
        var result = await _readinessScore.ComputeAsync(display, offlineOnboarding: false, muxWarning: null);
        ReadinessScoreValue = result.Score;
        ReadinessFactorsText = string.Join(" · ", result.Factors);
    }
}
