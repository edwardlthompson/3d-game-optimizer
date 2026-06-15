using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using SpatialLabsOptimizer.ViewModels;

namespace SpatialLabsOptimizer.Views;

public sealed partial class AboutView : Page
{
    public AboutViewModel ViewModel { get; private set; } = null!;

    public AboutView()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is not AboutViewModel vm)
        {
            return;
        }

        ViewModel = vm;
        Bindings.Update();
        await vm.LoadAsync();
    }
}
