using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using SpatialLabsOptimizer.Views;

namespace SpatialLabsOptimizer;

public sealed partial class MainWindow : Window
{
    public MainWindow(IServiceProvider services)
    {
        InitializeComponent();
        Content = services.GetRequiredService<ShellPage>();
    }
}
