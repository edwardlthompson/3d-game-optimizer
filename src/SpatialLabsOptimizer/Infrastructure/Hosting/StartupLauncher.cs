using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Dispatching;
using SpatialLabsOptimizer.Infrastructure;
using SpatialLabsOptimizer.Infrastructure.Pcvr;
using SpatialLabsOptimizer.Views;

namespace SpatialLabsOptimizer.Infrastructure.Hosting;

internal static class StartupLauncher
{
    public static async Task<IHost> RunAsync(
        SplashProgressReporter splash,
        CancellationToken cancellationToken = default)
    {
        var dispatcher = DispatcherQueue.GetForCurrentThread();

        await ReportAsync(dispatcher, splash, 5, "Starting 3D Game Optimizer...");
        await Task.Yield();

        await ReportAsync(dispatcher, splash, 15, "Initializing logging and privacy guard...");
        StartupDiagnostics.Trace("Building host");
        var host = await Task.Run(
            () => Host.CreateDefaultBuilder()
                .ConfigureSpatialLabsOptimizerServices()
                .Build(),
            cancellationToken);

        StartupDiagnostics.Trace("Host built");

        await ReportAsync(dispatcher, splash, 45, "Loading game library and launch services...");
        await ReportAsync(dispatcher, splash, 60, "Loading compatibility database...");

        await ReportAsync(dispatcher, splash, 72, "Checking command-line launch request...");
        if (ProtocolRegistrationService.TryParsePlayUri(
                ProtocolRegistrationService.FindProtocolUriInCommandLine(),
                out var appId))
        {
            App.PendingProtocolAppId = appId;
        }

        await ReportAsync(dispatcher, splash, 85, "Preparing main window...");
        await ReportAsync(dispatcher, splash, 100, "Ready");
        await Task.Delay(120, cancellationToken);

        return host;
    }

    private static Task ReportAsync(
        DispatcherQueue dispatcher,
        SplashProgressReporter splash,
        double percent,
        string message)
    {
        return RunOnUiThreadAsync(
            dispatcher,
            () => splash.ReportProgress(percent, message));
    }

    private static Task RunOnUiThreadAsync(DispatcherQueue dispatcher, Action action)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!dispatcher.TryEnqueue(() =>
            {
                try
                {
                    action();
                    tcs.SetResult();
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }))
        {
            tcs.SetException(new InvalidOperationException("UI dispatcher is unavailable."));
        }

        return tcs.Task;
    }
}
