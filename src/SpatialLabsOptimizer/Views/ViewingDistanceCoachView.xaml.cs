using Microsoft.Extensions.DependencyInjection;
using SpatialLabsOptimizer.Infrastructure.Displays;

namespace SpatialLabsOptimizer.Views;

public sealed partial class ViewingDistanceCoachView : Microsoft.UI.Xaml.Controls.UserControl
{
    private ViewingDistanceCoach? _coach;
    private string _profileId = "generic-manual";

    public ViewingDistanceCoachView()
    {
        InitializeComponent();
        Loaded += ViewingDistanceCoachView_Loaded;
    }

    public void SetProfile(string profileId)
    {
        _profileId = profileId;
        RefreshGuide();
    }

    private void ViewingDistanceCoachView_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _coach ??= App.Services.GetRequiredService<ViewingDistanceCoach>();
        RefreshGuide();
    }

    private void RefreshGuide()
    {
        if (_coach is null)
        {
            return;
        }

        var guide = _coach.GetGuideForProfile(_profileId);
        TipBlock.Text = guide.Tip;
        DistanceSlider.Value = guide.RecommendedDistanceCm;
        RangeBlock.Text = $"Recommended range: {guide.MinDistanceCm}–{guide.MaxDistanceCm} cm";
        UpdateFeedback((int)DistanceSlider.Value);
    }

    private void DistanceSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (_coach is null)
        {
            return;
        }

        UpdateFeedback((int)e.NewValue);
    }

    private void UpdateFeedback(int distanceCm)
    {
        if (_coach is null)
        {
            return;
        }

        FeedbackBlock.Text = _coach.EvaluateDistance(_profileId, distanceCm);
    }
}
