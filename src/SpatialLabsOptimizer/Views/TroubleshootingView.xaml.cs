using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using SpatialLabsOptimizer.ViewModels;

namespace SpatialLabsOptimizer.Views;

public sealed partial class TroubleshootingView : Page
{
    public TroubleshootingViewModel ViewModel { get; private set; } = null!;

    public TroubleshootingView()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is not TroubleshootingViewModel vm)
        {
            return;
        }

        ViewModel = vm;
        Bindings.Update();
        await vm.LoadAsync();
    }
}
