using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using SpatialLabsOptimizer.Infrastructure.Library;

namespace SpatialLabsOptimizer.ViewModels;

public sealed class LibrarySettingsViewModel : ViewModelBase
{
    private readonly PlatformConnectionService _connections;
    private readonly PlatformConnectionRepository _repository;
    private readonly PlatformLibraryStatsService _stats;
    private readonly LocalGameFolderRepository _folders;

    private string _steamId = "";
    private string _steamStatus = "Not connected";
    private string _epicPath = "";
    private string _epicStatus = "Not validated";
    private string _gogPath = "";
    private string _gogStatus = "Not validated";
    private string _ubisoftPath = "";
    private string _ubisoftStatus = "Not validated";
    private string _statsSummary = "";
    private string _folderStatus = "";
    private IReadOnlyList<string> _foldersList = Array.Empty<string>();

    public LibrarySettingsViewModel(
        PlatformConnectionService connections,
        PlatformConnectionRepository repository,
        PlatformLibraryStatsService stats,
        LocalGameFolderRepository folders)
    {
        _connections = connections;
        _repository = repository;
        _stats = stats;
        _folders = folders;

        TestSteamCommand = new RelayCommand(async () => await TestSteamConnectionAsync());
        ValidateEpicCommand = new RelayCommand(async () => await ValidateEpicConnectionAsync());
        ValidateGogCommand = new RelayCommand(async () => await ValidateGogConnectionAsync());
        ValidateUbisoftCommand = new RelayCommand(async () => await ValidateUbisoftConnectionAsync());
        RefreshStatsCommand = new RelayCommand(async () => await RefreshStatsAsync());
        OpenSteamKeyHelpCommand = new RelayCommand(OpenSteamKeyHelp);
    }

    public string SteamId
    {
        get => _steamId;
        set => SetProperty(ref _steamId, value);
    }

    public string SteamApiKey { get; set; } = "";

    public string SteamStatus
    {
        get => _steamStatus;
        set => SetProperty(ref _steamStatus, value);
    }

    public string EpicPath
    {
        get => _epicPath;
        set => SetProperty(ref _epicPath, value);
    }

    public string EpicStatus
    {
        get => _epicStatus;
        set => SetProperty(ref _epicStatus, value);
    }

    public string GogPath
    {
        get => _gogPath;
        set => SetProperty(ref _gogPath, value);
    }

    public string GogStatus
    {
        get => _gogStatus;
        set => SetProperty(ref _gogStatus, value);
    }

    public string UbisoftPath
    {
        get => _ubisoftPath;
        set => SetProperty(ref _ubisoftPath, value);
    }

    public string UbisoftStatus
    {
        get => _ubisoftStatus;
        set => SetProperty(ref _ubisoftStatus, value);
    }

    public string StatsSummary
    {
        get => _statsSummary;
        set => SetProperty(ref _statsSummary, value);
    }

    public string FolderStatus
    {
        get => _folderStatus;
        set => SetProperty(ref _folderStatus, value);
    }

    public IReadOnlyList<string> FoldersList
    {
        get => _foldersList;
        set => SetProperty(ref _foldersList, value);
    }

    public ICommand TestSteamCommand { get; }
    public ICommand ValidateEpicCommand { get; }
    public ICommand ValidateGogCommand { get; }
    public ICommand ValidateUbisoftCommand { get; }
    public ICommand RefreshStatsCommand { get; }
    public ICommand OpenSteamKeyHelpCommand { get; }

    public async Task LoadAsync()
    {
        SteamId = await _repository.GetSteamIdAsync() ?? "";
        EpicPath = await _repository.GetEpicManifestsPathAsync() ?? _connections.GetDefaultEpicManifestsPath();
        GogPath = await _repository.GetGogGamesPathAsync() ?? _connections.GetDefaultGogGamesPath();
        UbisoftPath = await _repository.GetUbisoftConfigPathAsync() ?? _connections.GetDefaultUbisoftConfigPath();
        var validated = await _repository.GetSteamLastValidatedUtcAsync();
        SteamStatus = validated.HasValue ? $"Last validated {validated.Value.ToLocalTime():g}" : "Not connected";
        await RefreshFoldersAsync();
        await RefreshStatsAsync();
    }

    public async Task RefreshFoldersAsync()
    {
        var folders = await _folders.GetFoldersAsync();
        FoldersList = folders;
        FolderStatus = folders.Count == 0
            ? "No custom folders configured."
            : $"{folders.Count} custom folder(s) watched.";
    }

    public async Task AddFolderAsync(string path)
    {
        await _folders.AddFolderAsync(path);
        await RunIndexAsync();
        await RefreshFoldersAsync();
        await RefreshStatsAsync();
    }

    public async Task RemoveFolderAsync(string path)
    {
        await _folders.RemoveFolderAsync(path);
        await RunIndexAsync();
        await RefreshFoldersAsync();
        await RefreshStatsAsync();
    }

    public async Task RescanFoldersAsync()
    {
        await RunIndexAsync();
        FolderStatus = "Local folder scan complete.";
        await RefreshStatsAsync();
    }

    public async Task SaveEpicPathAsync(string path)
    {
        await _repository.SetEpicManifestsPathAsync(path);
        EpicPath = path;
    }

    public async Task SaveGogPathAsync(string path)
    {
        await _repository.SetGogGamesPathAsync(path);
        GogPath = path;
    }

    public async Task SaveUbisoftPathAsync(string path)
    {
        await _repository.SetUbisoftConfigPathAsync(path);
        UbisoftPath = path;
    }

    public async Task TestSteamConnectionAsync()
    {
        var result = await _connections.ValidateSteamAsync(SteamId, SteamApiKey);
        SteamStatus = result.Message;
        if (result.Success)
        {
            SteamApiKey = "";
            await RunIndexAsync();
            await RefreshStatsAsync();
        }
    }

    public async Task ValidateEpicConnectionAsync()
    {
        await _repository.SetEpicManifestsPathAsync(EpicPath);
        var result = await _connections.ValidateEpicAsync();
        EpicStatus = result.Message;
        if (result.Success)
        {
            await RunIndexAsync();
            await RefreshStatsAsync();
        }
    }

    public async Task ValidateGogConnectionAsync()
    {
        await _repository.SetGogGamesPathAsync(GogPath);
        var result = await _connections.ValidateGogAsync();
        GogStatus = result.Message;
        if (result.Success)
        {
            await RunIndexAsync();
            await RefreshStatsAsync();
        }
    }

    public async Task ValidateUbisoftConnectionAsync()
    {
        await _repository.SetUbisoftConfigPathAsync(UbisoftPath);
        var result = await _connections.ValidateUbisoftAsync();
        UbisoftStatus = result.Message;
        if (result.Success)
        {
            await RunIndexAsync();
            await RefreshStatsAsync();
        }
    }

    public async Task RefreshStatsAsync()
    {
        var stats = await _stats.GetStatsAsync();
        StatsSummary =
            $"Steam: {stats.SteamInstalledLocal} installed locally, {stats.SteamOwnedOnline} owned online, {stats.SteamCompatibilitySeed} in compatibility seed\n" +
            $"Epic: {stats.EpicInstalledLocal} installed (online catalog — coming soon)\n" +
            $"GOG: {stats.GogInstalledLocal} installed (online catalog — coming soon)\n" +
            $"Ubisoft: {stats.UbisoftInstalledLocal} installed (online catalog — coming soon)\n" +
            $"Custom folders: {stats.CustomLocalFolders} games\n" +
            $"Total in library: {stats.TotalInLibrary}";
    }

    private static async Task RunIndexAsync()
    {
        var indexer = App.Services.GetRequiredService<LibraryIndexer>();
        await indexer.IndexAsync();
    }

    public void OpenSteamKeyHelp()
    {
        _ = global::System.Diagnostics.Process.Start(new global::System.Diagnostics.ProcessStartInfo
        {
            FileName = "https://steamcommunity.com/dev/apikey",
            UseShellExecute = true
        });
    }
}
