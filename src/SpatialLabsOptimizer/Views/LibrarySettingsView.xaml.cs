using Microsoft.Extensions.DependencyInjection;
using SpatialLabsOptimizer.ViewModels;
using WinRT.Interop;

namespace SpatialLabsOptimizer.Views;

public sealed partial class LibrarySettingsView : Microsoft.UI.Xaml.Controls.Page
{
    private LibrarySettingsViewModel? _viewModel;

    public LibrarySettingsView()
    {
        InitializeComponent();
        Loaded += LibrarySettingsView_Loaded;
    }

    private async void LibrarySettingsView_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel = App.Services.GetRequiredService<LibrarySettingsViewModel>();
        await _viewModel.LoadAsync();
        BindViewModel();
    }

    private void BindViewModel()
    {
        if (_viewModel is null)
        {
            return;
        }

        SteamIdBox.Text = _viewModel.SteamId;
        SteamStatusBlock.Text = _viewModel.SteamStatus;
        EpicPathBox.Text = _viewModel.EpicPath;
        EpicStatusBlock.Text = _viewModel.EpicStatus;
        GogPathBox.Text = _viewModel.GogPath;
        GogStatusBlock.Text = _viewModel.GogStatus;
        UbisoftPathBox.Text = _viewModel.UbisoftPath;
        UbisoftStatusBlock.Text = _viewModel.UbisoftStatus;
        StatsBlock.Text = _viewModel.StatsSummary;
        StatusBlock.Text = _viewModel.FolderStatus;
        FoldersList.ItemsSource = _viewModel.FoldersList;
    }

    private async void TestSteam_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.SteamId = SteamIdBox.Text;
        _viewModel.SteamApiKey = SteamKeyBox.Password;
        await _viewModel.TestSteamConnectionAsync();
        SteamStatusBlock.Text = _viewModel.SteamStatus;
        StatsBlock.Text = _viewModel.StatsSummary;
        SteamKeyBox.Password = string.Empty;
    }

    private void SteamKeyHelp_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        => _viewModel?.OpenSteamKeyHelpCommand.Execute(null);

    private async void ValidateEpic_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        await _viewModel.SaveEpicPathAsync(EpicPathBox.Text);
        await _viewModel.ValidateEpicConnectionAsync();
        EpicStatusBlock.Text = _viewModel.EpicStatus;
        StatsBlock.Text = _viewModel.StatsSummary;
    }

    private async void ValidateGog_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        await _viewModel.SaveGogPathAsync(GogPathBox.Text);
        await _viewModel.ValidateGogConnectionAsync();
        GogStatusBlock.Text = _viewModel.GogStatus;
        StatsBlock.Text = _viewModel.StatsSummary;
    }

    private async void ValidateUbisoft_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        await _viewModel.SaveUbisoftPathAsync(UbisoftPathBox.Text);
        await _viewModel.ValidateUbisoftConnectionAsync();
        UbisoftStatusBlock.Text = _viewModel.UbisoftStatus;
        StatsBlock.Text = _viewModel.StatsSummary;
    }

    private async void RefreshStats_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        await _viewModel.RefreshStatsAsync();
        StatsBlock.Text = _viewModel.StatsSummary;
    }

    private async void BrowseEpic_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var path = await PickFolderAsync();
        if (path is not null && _viewModel is not null)
        {
            EpicPathBox.Text = path;
            await _viewModel.SaveEpicPathAsync(path);
        }
    }

    private async void BrowseGog_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var path = await PickFolderAsync();
        if (path is not null && _viewModel is not null)
        {
            GogPathBox.Text = path;
            await _viewModel.SaveGogPathAsync(path);
        }
    }

    private async void BrowseUbisoft_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var path = await PickFolderAsync();
        if (path is not null && _viewModel is not null)
        {
            UbisoftPathBox.Text = path;
            await _viewModel.SaveUbisoftPathAsync(path);
        }
    }

    private async void AddFolder_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var path = await PickFolderAsync();
        if (path is not null && _viewModel is not null)
        {
            await _viewModel.AddFolderAsync(path);
            BindViewModel();
        }
    }

    private async void RemoveFolder_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_viewModel is null || FoldersList.SelectedItem is not string path)
        {
            StatusBlock.Text = "Select a folder to remove.";
            return;
        }

        await _viewModel.RemoveFolderAsync(path);
        BindViewModel();
    }

    private async void RefreshFolders_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_viewModel is not null)
        {
            await _viewModel.RescanFoldersAsync();
            BindViewModel();
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
