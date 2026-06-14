using Microsoft.Extensions.DependencyInjection;
using SpatialLabsOptimizer.Infrastructure.Performance;

namespace SpatialLabsOptimizer.Views;

public sealed partial class Global3DSettingsView : Microsoft.UI.Xaml.Controls.Page
{
    public Global3DSettingsView()
    {
        InitializeComponent();
    }

    private async void Benchmark_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var benchmark = App.Services.GetRequiredService<BenchmarkService>();
        var score = await benchmark.RunBenchmarkAsync();
        BenchmarkResult.Text = $"Benchmark score: {score:F0}";
    }
}
