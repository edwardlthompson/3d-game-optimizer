using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SpatialLabsOptimizer.Controls;

public sealed partial class LaunchProgressOverlay : UserControl
{
    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(LaunchProgressOverlay),
            new PropertyMetadata(false, OnIsOpenChanged));

    public static readonly DependencyProperty GameTitleProperty =
        DependencyProperty.Register(nameof(GameTitle), typeof(string), typeof(LaunchProgressOverlay),
            new PropertyMetadata(string.Empty, OnGameTitleChanged));

    public static readonly DependencyProperty CurrentStepProperty =
        DependencyProperty.Register(nameof(CurrentStep), typeof(string), typeof(LaunchProgressOverlay),
            new PropertyMetadata(string.Empty, OnCurrentStepChanged));

    public static readonly DependencyProperty ProgressPercentProperty =
        DependencyProperty.Register(nameof(ProgressPercent), typeof(double), typeof(LaunchProgressOverlay),
            new PropertyMetadata(0d, OnProgressPercentChanged));

    public LaunchProgressOverlay()
    {
        InitializeComponent();
    }

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public string GameTitle
    {
        get => (string)GetValue(GameTitleProperty);
        set => SetValue(GameTitleProperty, value);
    }

    public string CurrentStep
    {
        get => (string)GetValue(CurrentStepProperty);
        set => SetValue(CurrentStepProperty, value);
    }

    public double ProgressPercent
    {
        get => (double)GetValue(ProgressPercentProperty);
        set => SetValue(ProgressPercentProperty, value);
    }

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LaunchProgressOverlay overlay)
        {
            overlay.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private static void OnGameTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LaunchProgressOverlay overlay)
        {
            overlay.GameTitleBlock.Text = e.NewValue as string ?? "";
        }
    }

    private static void OnCurrentStepChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LaunchProgressOverlay overlay)
        {
            overlay.StepBlock.Text = e.NewValue as string ?? "";
        }
    }

    private static void OnProgressPercentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not LaunchProgressOverlay overlay)
        {
            return;
        }

        var percent = e.NewValue is double value ? value : 0d;
        overlay.LaunchProgressBar.IsIndeterminate = percent <= 0;
        overlay.LaunchProgressBar.Value = Math.Clamp(percent, 0, 100);
    }
}
