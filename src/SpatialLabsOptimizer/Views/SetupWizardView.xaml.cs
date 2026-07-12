using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using SpatialLabsOptimizer.ViewModels;

namespace SpatialLabsOptimizer.Views;

public sealed partial class SetupWizardView : Page
{
    public SetupWizardViewModel ViewModel { get; private set; } = null!;

    public SetupWizardView()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is not SetupWizardViewModel vm)
        {
            return;
        }

        ViewModel = vm;
        ViewModel.Finished -= OnFinished;
        ViewModel.Finished += OnFinished;
        Bindings.Update();
        await ViewModel.LoadAsync();
        DisplayPicker.SetCatalog(ViewModel.Toolchain.DisplayCatalog);
        if (ViewModel.Toolchain.DetectedDisplayId is { } id)
        {
            DisplayPicker.SelectProfileById(id);
        }

        DisplayPicker.SelectionChanged -= DisplayPicker_SelectionChanged;
        DisplayPicker.SelectionChanged += DisplayPicker_SelectionChanged;
        ToolchainPanel.Bind(ViewModel.Toolchain);
    }

    private async void DisplayPicker_SelectionChanged(object? sender, EventArgs e)
    {
        if (DisplayPicker.SelectedDisplay is { } profile)
        {
            await ViewModel.OnDisplaySelectedAsync(profile);
        }
    }

    private void OnFinished()
    {
        ShellPage.Current?.NavigateToTag("library");
    }
}
