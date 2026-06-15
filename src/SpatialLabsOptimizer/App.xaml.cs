using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using SpatialLabsOptimizer.Infrastructure;
using SpatialLabsOptimizer.Infrastructure.Hosting;
using SpatialLabsOptimizer.Infrastructure.Pcvr;
using SpatialLabsOptimizer.Views;

namespace SpatialLabsOptimizer;

public partial class App : Microsoft.UI.Xaml.Application
{
    private IHost? _host;

    public static int? PendingProtocolAppId { get; set; }

    public MainWindow? PrimaryWindow { get; private set; }

    public static IServiceProvider Services =>
        ((App)Current)._host?.Services
        ?? throw new InvalidOperationException("Application host is not initialized.");

    public App()
    {
        StartupDiagnostics.Trace("App constructor begin");
        InitializeComponent();
        UnhandledException += (_, e) =>
        {
            StartupDiagnostics.WriteFailure($"UnhandledException: {e.Exception}");
            e.Handled = false;
        };
        StartupDiagnostics.Trace("App constructor complete");
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        StartupDiagnostics.Trace("OnLaunched begin");

        var enqueued = DispatcherQueue.GetForCurrentThread().TryEnqueue(StartApplication);
        if (!enqueued)
        {
            StartupDiagnostics.WriteFailure("Failed to enqueue StartApplication.");
        }

        StartupDiagnostics.Trace("OnLaunched returning");
    }

    private void StartApplication()
    {
        StartupDiagnostics.Trace("StartApplication begin");

        try
        {
            var window = new MainWindow();
            PrimaryWindow = window;
            StartupDiagnostics.Trace("MainWindow constructed");
            window.ShowSplash();
            StartupDiagnostics.Trace("Splash content set");
            window.Activate();
            StartupDiagnostics.Trace("MainWindow activated with splash");

            _ = FinishStartupAsync(window);
        }
        catch (Exception ex)
        {
            StartupDiagnostics.WriteFailure($"StartApplication failed: {ex}");
        }
    }

    private async Task FinishStartupAsync(MainWindow window)
    {
        try
        {
            StartupDiagnostics.Trace("FinishStartupAsync begin");
            _host = await Task.Run(
                () => Host.CreateDefaultBuilder()
                    .ConfigureSpatialLabsOptimizerServices()
                    .Build());
            StartupDiagnostics.Trace("Host built");

            if (ProtocolRegistrationService.TryParsePlayUri(
                    ProtocolRegistrationService.FindProtocolUriInCommandLine(),
                    out var appId))
            {
                PendingProtocolAppId = appId;
            }

            await window.DispatcherQueue.EnqueueAsync(() =>
            {
                window.ShowShell(_host.Services);
                StartupDiagnostics.Trace("Main shell shown; startup complete");
            });
        }
        catch (Exception ex)
        {
            StartupDiagnostics.WriteFailure($"FinishStartupAsync failed: {ex}");
            window.SplashProgress.ReportError(
                $"Startup failed: {ex.Message}{Environment.NewLine}" +
                $"Details were written to {StartupDiagnostics.LogDirectory}.");
        }
    }
}
