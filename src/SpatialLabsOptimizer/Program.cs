using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.ApplicationModel.DynamicDependency;
using SpatialLabsOptimizer.Infrastructure;
using WinRT;
using WinUIApplication = Microsoft.UI.Xaml.Application;

namespace SpatialLabsOptimizer;

public static class Program
{
    // Windows App SDK 2.2 (matches Microsoft.WindowsAppSDK 2.2.x)
    private const uint WindowsAppSdkVersion = 0x00020002;

    [STAThread]
    public static void Main(string[] args)
    {
        StartupDiagnostics.Trace("Main entered");

        if (!Bootstrap.TryInitialize(WindowsAppSdkVersion, out var bootstrapHr) || bootstrapHr != 0)
        {
            StartupDiagnostics.WriteFailure(
                $"Bootstrap.TryInitialize failed: hr=0x{bootstrapHr:X8}");
            Environment.Exit(bootstrapHr != 0 ? bootstrapHr : 1);
            return;
        }

        StartupDiagnostics.Trace("Bootstrap initialized");
        var priPath = Path.Combine(AppContext.BaseDirectory, "SpatialLabsOptimizer.pri");
        StartupDiagnostics.Trace(
            $"PriPresent={File.Exists(priPath)} BaseDir={AppContext.BaseDirectory}");

        try
        {
            XamlCheckProcessRequirements();
            ComWrappersSupport.InitializeComWrappers();
            StartupDiagnostics.Trace("Starting WinUI application");

            WinUIApplication.Start(_ =>
            {
                StartupDiagnostics.Trace("WinUI Application.Start callback");
                var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
                SynchronizationContext.SetSynchronizationContext(
                    new DispatcherQueueSynchronizationContext(dispatcherQueue));

                new App();
                StartupDiagnostics.Trace("App constructed");
            });

            StartupDiagnostics.Trace("WinUI message loop exited");
        }
        catch (Exception ex)
        {
            StartupDiagnostics.WriteFailure(ex.ToString());
            throw;
        }
        finally
        {
            Bootstrap.Shutdown();
        }
    }

    [DllImport("Microsoft.ui.xaml.dll")]
    private static extern void XamlCheckProcessRequirements();
}
