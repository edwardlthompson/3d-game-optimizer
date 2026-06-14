using Microsoft.Extensions.DependencyInjection;
using SpatialLabsOptimizer.Infrastructure.Pcvr;

namespace SpatialLabsOptimizer.Views;

public sealed partial class TroubleshootingView : Microsoft.UI.Xaml.Controls.Page
{
    public TroubleshootingView()
    {
        InitializeComponent();
    }

    private async void Export_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var diagnostics = App.Services.GetRequiredService<DiagnosticBundleService>();
        var path = await diagnostics.ExportAsync();
        ExportPathBlock.Text = $"Exported to: {path}";
    }
}
