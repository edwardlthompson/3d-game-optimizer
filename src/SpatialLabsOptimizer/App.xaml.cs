using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using SpatialLabsOptimizer.Infrastructure.Hosting;
using SpatialLabsOptimizer.Infrastructure.Pcvr;

namespace SpatialLabsOptimizer;

public partial class App : Microsoft.UI.Xaml.Application
{
    private IHost? _host;

    public static int? PendingProtocolAppId { get; set; }

    public Window? MainWindow { get; private set; }

    public static IServiceProvider Services =>
        ((App)Current)._host?.Services
        ?? throw new InvalidOperationException("Application host is not initialized.");

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureSpatialLabsOptimizerServices()
            .Build();

        if (ProtocolRegistrationService.TryParsePlayUri(
                ProtocolRegistrationService.FindProtocolUriInCommandLine(),
                out var appId))
        {
            PendingProtocolAppId = appId;
        }

        MainWindow = _host.Services.GetRequiredService<MainWindow>();
        MainWindow.Activate();
    }
}
