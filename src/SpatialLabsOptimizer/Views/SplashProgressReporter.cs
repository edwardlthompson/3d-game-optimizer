using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace SpatialLabsOptimizer.Views;

public sealed class SplashProgressReporter
{
    private readonly Window _window;
    private readonly ProgressBar? _progressBar;
    private readonly TextBlock? _statusText;

    public SplashProgressReporter(Window window)
    {
        _window = window;
        if (window.Content is Grid root)
        {
            _progressBar = FindChild<ProgressBar>(root, "SplashProgress");
            _statusText = FindChild<TextBlock>(root, "SplashStatus");
        }
    }

    public void ReportProgress(double percent, string message)
    {
        if (_progressBar is not null)
        {
            _progressBar.IsIndeterminate = false;
            _progressBar.Value = Math.Clamp(percent, 0, 100);
        }

        if (_statusText is not null)
        {
            _statusText.Text = message;
        }
    }

    public void ReportError(string message)
    {
        if (_statusText is not null)
        {
            _statusText.Text = message;
            _statusText.Foreground = new SolidColorBrush(Colors.IndianRed);
        }
    }

    private static T? FindChild<T>(DependencyObject parent, string name)
        where T : FrameworkElement
    {
        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match && match.Name == name)
            {
                return match;
            }

            var nested = FindChild<T>(child, name);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }
}
