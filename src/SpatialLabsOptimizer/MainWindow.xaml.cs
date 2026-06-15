using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using SpatialLabsOptimizer.Infrastructure;
using SpatialLabsOptimizer.Infrastructure.Hosting;
using SpatialLabsOptimizer.Views;
using WinRT.Interop;

namespace SpatialLabsOptimizer;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        StartupDiagnostics.Trace("MainWindow ctor begin");
        InitializeComponent();
        TrySetWindowIcon();
        StartupDiagnostics.Trace("MainWindow InitializeComponent done");
    }

    public SplashProgressReporter SplashProgress => new(this);

    public void ShowSplash()
    {
        StartupDiagnostics.Trace("MainWindow ShowSplash");
        Content = SplashContentFactory.CreateRoot();
    }

    public void ShowShell(IServiceProvider services)
    {
        StartupDiagnostics.Trace("MainWindow ShowShell begin");
        UiThreadDispatcher.Enqueue = action => DispatcherQueue.TryEnqueue(() => action());
        Content = services.GetRequiredService<ShellPage>();
        StartupDiagnostics.Trace("MainWindow ShowShell complete");
    }

    private void TrySetWindowIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
        if (!File.Exists(iconPath))
        {
            return;
        }

        try
        {
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            AppWindow.GetFromWindowId(windowId).SetIcon(iconPath);
            StartupDiagnostics.Trace("Window icon applied");
        }
        catch (Exception ex)
        {
            StartupDiagnostics.Trace($"SetIcon failed: {ex.Message}");
        }
    }
}
