using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using SpatialLabsOptimizer.ViewModels;

namespace SpatialLabsOptimizer.Views;

public sealed partial class GlossaryView : Page
{
    public GlossaryViewModel ViewModel { get; private set; } = null!;

    public GlossaryView()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is not GlossaryViewModel vm)
        {
            return;
        }

        ViewModel = vm;
        Bindings.Update();
        await vm.LoadAsync();
    }
}
