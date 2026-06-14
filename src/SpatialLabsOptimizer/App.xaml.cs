using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using SpatialLabsOptimizer.Infrastructure.Hosting;

namespace SpatialLabsOptimizer;

public partial class App : Microsoft.UI.Xaml.Application
{
    private IHost? _host;

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

        MainWindow = _host.Services.GetRequiredService<MainWindow>();
        MainWindow.Activate();
    }
}
