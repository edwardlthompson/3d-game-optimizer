using Microsoft.Extensions.DependencyInjection;
using SpatialLabsOptimizer.Infrastructure.Library;
using WinRT.Interop;

namespace SpatialLabsOptimizer.Views;

public sealed partial class LibrarySettingsView : Microsoft.UI.Xaml.Controls.Page
{
    public LibrarySettingsView()
    {
        InitializeComponent();
        Loaded += LibrarySettingsView_Loaded;
    }

    private async void LibrarySettingsView_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await RefreshListAsync();
    }

    private async Task RefreshListAsync()
    {
        var repo = App.Services.GetRequiredService<LocalGameFolderRepository>();
        var folders = await repo.GetFoldersAsync();
        FoldersList.ItemsSource = folders;
        StatusBlock.Text = folders.Count == 0
            ? "No local folders configured."
            : $"{folders.Count} folder(s) watched — click Refresh scan after adding games.";
    }

    private async void AddFolder_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var picker = new Windows.Storage.Pickers.FolderPicker();
        picker.FileTypeFilter.Add("*");
        if (App.Current is App app && app.MainWindow is not null)
        {
            var hwnd = WindowNative.GetWindowHandle(app.MainWindow);
            InitializeWithWindow.Initialize(picker, hwnd);
        }

        var folder = await picker.PickSingleFolderAsync();
        if (folder is null)
        {
            return;
        }

        var repo = App.Services.GetRequiredService<LocalGameFolderRepository>();
        await repo.AddFolderAsync(folder.Path);
        await RunLocalScanAsync();
        await RefreshListAsync();
    }

    private async void RemoveFolder_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (FoldersList.SelectedItem is not string path)
        {
            StatusBlock.Text = "Select a folder to remove.";
            return;
        }

        var repo = App.Services.GetRequiredService<LocalGameFolderRepository>();
        await repo.RemoveFolderAsync(path);
        await RunLocalScanAsync();
        await RefreshListAsync();
    }

    private async void RefreshFolders_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await RunLocalScanAsync();
        StatusBlock.Text = "Local folder scan complete.";
    }

    private static async Task RunLocalScanAsync()
    {
        var indexer = App.Services.GetRequiredService<LibraryIndexer>();
        await indexer.IndexAsync();
    }
}
