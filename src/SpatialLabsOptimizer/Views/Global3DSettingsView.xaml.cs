using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using SpatialLabsOptimizer.ViewModels;

namespace SpatialLabsOptimizer.Views;

public sealed partial class Global3DSettingsView : Page
{
    public Global3DSettingsViewModel ViewModel { get; private set; } = null!;

    public Global3DSettingsView()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is not Global3DSettingsViewModel vm)
        {
            return;
        }

        ViewModel = vm;
        Bindings.Update();
        await vm.LoadAsync();
        ToolchainPanel.Bind(vm.Toolchain);
        ToolchainExpander.IsExpanded = vm.ExpandToolchain || ToolchainExpander.IsExpanded;
        ViewingDistanceCoachPanel.Initialize(vm.ViewingDistanceCoach);
        ViewingDistanceCoachPanel.SetProfile("generic-manual");
    }

    private void LibrarySettings_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        => ShellPage.Current?.NavigateToTag("library-settings");
}
