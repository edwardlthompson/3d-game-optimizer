using Microsoft.UI.Xaml;
using SpatialLabsOptimizer.Domain;

namespace SpatialLabsOptimizer.Infrastructure.Responsive;

public sealed class ResponsiveStateService
{
    private ResponsiveBreakpoint _current = ResponsiveBreakpoint.Medium;
    private int _currentColumns = 4;

    public ResponsiveBreakpoint CurrentBreakpoint => _current;
    public int CurrentColumns => _currentColumns;

    public event EventHandler? StateChanged;

    public void UpdateFromWidth(double width)
    {
        var (breakpoint, columns) = width switch
        {
            < 900 => (ResponsiveBreakpoint.Narrow, 2),
            < 1200 => (ResponsiveBreakpoint.Medium, 3),
            _ => (ResponsiveBreakpoint.Wide, 4)
        };

        if (_current == breakpoint && _currentColumns == columns)
        {
            return;
        }

        _current = breakpoint;
        _currentColumns = columns;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void AttachToWindow(Window window)
    {
        if (window.Content is FrameworkElement root)
        {
            root.SizeChanged += (_, args) => UpdateFromWidth(args.NewSize.Width);
            UpdateFromWidth(root.ActualWidth);
        }
    }
}
