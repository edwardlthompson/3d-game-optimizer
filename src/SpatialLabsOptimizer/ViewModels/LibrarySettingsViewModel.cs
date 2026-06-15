using System.Windows.Input;
using SpatialLabsOptimizer.Infrastructure.Library;

namespace SpatialLabsOptimizer.ViewModels;

public sealed partial class LibrarySettingsViewModel : ViewModelBase
{
    private readonly PlatformConnectionService _connections;
    private readonly PlatformConnectionRepository _repository;
    private readonly PlatformLibraryStatsService _stats;
    private readonly LocalGameFolderRepository _folders;
    private readonly LibraryIndexer _indexer;

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
    private string? _selectedFolder;

    public LibrarySettingsViewModel(
        PlatformConnectionService connections,
        PlatformConnectionRepository repository,
        PlatformLibraryStatsService stats,
        LocalGameFolderRepository folders,
        LibraryIndexer indexer)
    {
        _connections = connections;
        _repository = repository;
        _stats = stats;
        _folders = folders;
        _indexer = indexer;

        TestSteamCommand = new RelayCommand(async () => await TestSteamConnectionAsync());
        ValidateEpicCommand = new RelayCommand(async () => await ValidateEpicConnectionAsync());
        ValidateGogCommand = new RelayCommand(async () => await ValidateGogConnectionAsync());
        ValidateUbisoftCommand = new RelayCommand(async () => await ValidateUbisoftConnectionAsync());
        RefreshStatsCommand = new RelayCommand(async () => await RefreshStatsAsync());
        OpenSteamKeyHelpCommand = new RelayCommand(OpenSteamKeyHelp);
        RescanFoldersCommand = new RelayCommand(async () => await RescanFoldersAsync());
        RemoveSelectedFolderCommand = new RelayCommand(
            async () => await RemoveSelectedFolderAsync(),
            () => !string.IsNullOrWhiteSpace(SelectedFolder));
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

    public string? SelectedFolder
    {
        get => _selectedFolder;
        set
        {
            if (SetProperty(ref _selectedFolder, value))
            {
                ((RelayCommand)RemoveSelectedFolderCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand TestSteamCommand { get; }
    public ICommand ValidateEpicCommand { get; }
    public ICommand ValidateGogCommand { get; }
    public ICommand ValidateUbisoftCommand { get; }
    public ICommand RefreshStatsCommand { get; }
    public ICommand OpenSteamKeyHelpCommand { get; }
    public ICommand RescanFoldersCommand { get; }
    public ICommand RemoveSelectedFolderCommand { get; }
}
