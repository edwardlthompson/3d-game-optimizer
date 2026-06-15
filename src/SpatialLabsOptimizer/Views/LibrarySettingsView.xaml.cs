using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using SpatialLabsOptimizer.ViewModels;
using WinRT.Interop;

namespace SpatialLabsOptimizer.Views;

public sealed partial class LibrarySettingsView : Page
{
    public LibrarySettingsViewModel ViewModel { get; private set; } = null!;

    public LibrarySettingsView()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is not LibrarySettingsViewModel vm)
        {
            return;
        }

        ViewModel = vm;
        Bindings.Update();
        await vm.LoadAsync();
    }

    private async void TestSteam_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ViewModel.SteamApiKey = SteamKeyBox.Password;
        await ViewModel.TestSteamConnectionAsync();
        SteamKeyBox.Password = string.Empty;
    }

    private async void BrowseEpic_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var path = await PickFolderAsync();
        if (path is not null)
        {
            await ViewModel.SaveEpicPathAsync(path);
        }
    }

    private async void BrowseGog_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var path = await PickFolderAsync();
        if (path is not null)
        {
            await ViewModel.SaveGogPathAsync(path);
        }
    }

    private async void BrowseUbisoft_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var path = await PickFolderAsync();
        if (path is not null)
        {
            await ViewModel.SaveUbisoftPathAsync(path);
        }
    }

    private async void AddFolder_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var path = await PickFolderAsync();
        if (path is not null)
        {
            await ViewModel.AddFolderAsync(path);
        }
    }

    private static async Task<string?> PickFolderAsync()
    {
        var picker = new Windows.Storage.Pickers.FolderPicker();
        picker.FileTypeFilter.Add("*");
        if (App.Current is App app && app.PrimaryWindow is not null)
        {
            var hwnd = WindowNative.GetWindowHandle(app.PrimaryWindow);
            InitializeWithWindow.Initialize(picker, hwnd);
        }

        var folder = await picker.PickSingleFolderAsync();
        return folder?.Path;
    }
}
