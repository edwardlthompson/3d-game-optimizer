using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace SpatialLabsOptimizer.Views;

internal static class SplashContentFactory
{
    public static Grid CreateRoot()
    {
        return new Grid
        {
            Background = new SolidColorBrush(ColorHelper.FromArgb(255, 18, 18, 18)),
            Children =
            {
                new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Spacing = 12,
                    Children =
                    {
                        new Image
                        {
                            Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(
                                new Uri("ms-appx:///Assets/SplashLogo.png")),
                            Width = 96,
                            Height = 96,
                            HorizontalAlignment = HorizontalAlignment.Center,
                        },
                        new TextBlock
                        {
                            Text = "3D Game Optimizer",
                            FontSize = 26,
                            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 244, 244, 244)),
                        },
                        new ProgressBar
                        {
                            Name = "SplashProgress",
                            Minimum = 0,
                            Maximum = 100,
                            Width = 320,
                            Height = 4,
                            Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 255, 140, 0)),
                        },
                        new TextBlock
                        {
                            Name = "SplashStatus",
                            Text = "Starting...",
                            TextAlignment = TextAlignment.Center,
                            TextWrapping = TextWrapping.Wrap,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 244, 244, 244)),
                        },
                    },
                },
            },
        };
    }
}
