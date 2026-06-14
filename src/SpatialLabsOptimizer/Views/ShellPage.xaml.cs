using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using SpatialLabsOptimizer.Infrastructure.Responsive;
using SpatialLabsOptimizer.ViewModels;

namespace SpatialLabsOptimizer.Views;

public sealed partial class ShellPage : Page
{
    private readonly ResponsiveStateService _responsive;
    private readonly GameLibraryViewModel _libraryViewModel;
    private readonly SetupWizardViewModel _wizardViewModel;

    public ShellViewModel ViewModel { get; }

    public ShellPage(
        ShellViewModel viewModel,
        ResponsiveStateService responsive,
        GameLibraryViewModel libraryViewModel,
        SetupWizardViewModel wizardViewModel)
    {
        ViewModel = viewModel;
        _responsive = responsive;
        _libraryViewModel = libraryViewModel;
        _wizardViewModel = wizardViewModel;
        InitializeComponent();
        Loaded += ShellPage_Loaded;
    }

    private async void ShellPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (App.Current is App app && app.MainWindow is not null)
        {
            _responsive.AttachToWindow(app.MainWindow);
        }

        await ViewModel.InitializeAsync();
        NavView.SelectedItem = NavView.MenuItems[0];
        ContentFrame.Navigate(typeof(GameLibraryView), _libraryViewModel);
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item || item.Tag is not string tag)
        {
            return;
        }

        var pageType = tag switch
        {
            "library" => typeof(GameLibraryView),
            "wizard" => typeof(SetupWizardView),
            "settings" => typeof(Global3DSettingsView),
            "health" => typeof(ToolchainHealthView),
            "troubleshoot" => typeof(TroubleshootingView),
            "about" => typeof(AboutView),
            _ => typeof(GameLibraryView)
        };

        var parameter = tag switch
        {
            "library" => (object)_libraryViewModel,
            "wizard" => _wizardViewModel,
            _ => null!
        };

        ContentFrame.Navigate(pageType, parameter);
    }
}
