using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Views;

namespace SpatialLabsOptimizer.Infrastructure.Hosting;

public sealed class LaunchConfirmationService : ILaunchConfirmationService
{
    public async Task<bool> ConfirmAsync(LaunchPreviewSummary summary, CancellationToken cancellationToken = default)
    {
        var shell = ShellPage.Current;
        if (shell is null)
        {
            return true;
        }

        var body = new StackPanel { Spacing = 8 };
        body.Children.Add(new TextBlock
        {
            Text = summary.Title,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        });
        body.Children.Add(new TextBlock { Text = summary.PlatformLine, TextWrapping = TextWrapping.Wrap });
        body.Children.Add(new TextBlock { Text = summary.DepthLine, TextWrapping = TextWrapping.Wrap });
        body.Children.Add(new TextBlock { Text = summary.ToolchainLine, TextWrapping = TextWrapping.Wrap });
        body.Children.Add(new TextBlock { Text = summary.TierLine, TextWrapping = TextWrapping.Wrap });
        body.Children.Add(new TextBlock
        {
            Text = "Display support is not the same as full game support — see Glossary for tiers.",
            Opacity = 0.75,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap
        });

        var dialog = new ContentDialog
        {
            Title = "Confirm Play in 3D",
            Content = body,
            PrimaryButtonText = "Launch",
            SecondaryButtonText = "Glossary",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = shell.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Secondary)
        {
            shell.NavigateToTag("glossary");
            return false;
        }

        return result == ContentDialogResult.Primary;
    }
}
