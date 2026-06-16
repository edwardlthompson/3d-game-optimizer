using System.Windows.Input;
using SpatialLabsOptimizer.Infrastructure.Artwork;
using SpatialLabsOptimizer.Application.UseCases;
using SpatialLabsOptimizer.Infrastructure.Compatibility;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Library;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Responsive;
using SpatialLabsOptimizer.Infrastructure.Settings;
using SpatialLabsOptimizer.Infrastructure.Updates;

namespace SpatialLabsOptimizer.ViewModels;

public sealed class RecentLaunchItemViewModel
{
    public RecentLaunchItemViewModel(RecentLaunchEntry entry)
    {
        SteamAppId = entry.StableAppId;
        Title = entry.Title;
        LaunchedAtDisplay = entry.LaunchedAt.ToLocalTime().ToString("g");
        ResultDisplay = entry.Success ? "OK" : entry.ErrorCode ?? "Failed";
    }

    public int SteamAppId { get; }
    public string Title { get; }
    public string LaunchedAtDisplay { get; }
    public string ResultDisplay { get; }
}

public sealed partial class GameLibraryViewModel : ViewModelBase
{
    private readonly GameDatabase _database;
    private readonly PlayIn3D _playIn3D;
    private readonly PlayInVR _playInVr;
    private readonly LibrarySortService _sortService;
    private readonly ResponsiveStateService _responsive;
    private readonly ShellViewModel _shell;
    private readonly IncrementalSteamScanService? _incrementalScan;
    private readonly HdrWatchdogService? _hdrWatchdog;
    private readonly PinnedShelfRepository _pinnedShelf;
    private readonly PlayQueueService _playQueue;
    private readonly GameOverrideRepository _overrides;
    private readonly LocalPlaylistRepository _playlists;
    private readonly LibraryIntelligenceService _intelligence;
    private readonly PresetCacheService _presets;
    private readonly OperationProgressHub _progressHub;
    private readonly UserPreferencesService _preferences;
    private readonly LibraryPrefetchService _prefetch;
    private readonly CoverArtCache _coverCache;
    private readonly GameArtworkService _artwork;
    private readonly LibraryIndexer _indexer;
    private readonly CompatibilityRepository _compatibility;

    private IReadOnlyList<GameLibraryItemViewModel> _games = Array.Empty<GameLibraryItemViewModel>();
    private IReadOnlyList<string> _playlistNames = Array.Empty<string>();
    private IReadOnlyList<RecentLaunchItemViewModel> _recentLaunches = Array.Empty<RecentLaunchItemViewModel>();
    private LibrarySortMode _sortMode = LibrarySortMode.GameRank;
    private SmartCollectionMode _smartCollection = SmartCollectionMode.None;
    private string _warmStartStatus = "";
    private string _preferredOutput = "Auto";
    private string _playlistName = "";
    private string _compatibilityNote = "";
    private string _selectedPresetFreshness = "";
    private string _whyNotReadyHint = "";
    private string _selectedRecommendedTools = "";
    private string _selectedRank3DDisplay = "";
    private bool _showFavoritesOnly;
    private bool _showLocalOnly;
    private bool _showWhyNotReady;
    private bool _filterUltraNative;
    private bool _filterTrueGame;
    private bool _filterUevr;
    private bool _filter3DVision;
    private int _minRank3DScore;
    private bool _libraryPrefsLoaded;
    private CancellationTokenSource? _prefsSaveCts;

    public GameLibraryViewModel(
        GameDatabase database,
        PlayIn3D playIn3D,
        PlayInVR playInVr,
        LibrarySortService sortService,
        ResponsiveStateService responsive,
        ShellViewModel shell,
        PinnedShelfRepository pinnedShelf,
        PlayQueueService playQueue,
        GameOverrideRepository overrides,
        LocalPlaylistRepository playlists,
        LibraryIntelligenceService intelligence,
        PresetCacheService presets,
        OperationProgressHub progressHub,
        UserPreferencesService preferences,
        LibraryPrefetchService prefetch,
        CoverArtCache coverCache,
        GameArtworkService artwork,
        LibraryIndexer indexer,
        CompatibilityRepository compatibility,
        IncrementalSteamScanService? incrementalScan = null,
        HdrWatchdogService? hdrWatchdog = null,
        WorkshopPresetImporter? workshopImporter = null,
        LanPartyExportService? lanExport = null,
        HybridSessionService? hybridSession = null,
        ThreeDGoCodeService? codes = null)
    {
        _database = database;
        _playIn3D = playIn3D;
        _playInVr = playInVr;
        _sortService = sortService;
        _responsive = responsive;
        _shell = shell;
        _pinnedShelf = pinnedShelf;
        _playQueue = playQueue;
        _overrides = overrides;
        _playlists = playlists;
        _intelligence = intelligence;
        _presets = presets;
        _progressHub = progressHub;
        _preferences = preferences;
        _prefetch = prefetch;
        _coverCache = coverCache;
        _artwork = artwork;
        _indexer = indexer;
        _compatibility = compatibility;
        _incrementalScan = incrementalScan;
        _hdrWatchdog = hdrWatchdog;

        PlayCommand = new RelayCommand(async () => await PlaySelectedAsync());
        PlayVrCommand = new RelayCommand(async () => await PlayVrSelectedAsync());
        RefreshLibraryCommand = new RelayCommand(async () => await RefreshLibraryAsync());
        RefreshCoverArtCommand = new RelayCommand(async () => await RefreshCoverArtAsync());
        RefreshCommand = RefreshLibraryCommand;
        PinCommand = new RelayCommand(async () => await PinSelectedAsync());
        UnpinCommand = new RelayCommand(async () => await UnpinSelectedAsync());
        QueueCommand = new RelayCommand(async () => await EnqueueSelectedAsync());
        PlayNextCommand = new RelayCommand(async () => await PlayNextAsync());
        ToggleFavoriteCommand = new RelayCommand(async () => await ToggleFavoriteSelectedAsync());
        SavePlaylistCommand = new RelayCommand(async () => await SavePlaylistAsync());
        LoadPlaylistCommand = new RelayCommand(async () => await LoadPlaylistAsync());
        SaveOutputCommand = new RelayCommand(async () => await SavePreferredOutputAsync());
        SaveCompatibilityNoteCommand = new RelayCommand(async () => await SaveCompatibilityNoteAsync());
        RefreshPresetCommand = new RelayCommand(async () => await RefreshSelectedPresetAsync());
        OpenCatalogCommand = new RelayCommand(async () => await OpenCatalogSiteAsync());
        _workshopImporter = workshopImporter;
        _lanExport = lanExport;
        _hybridSession = hybridSession;
        _codes = codes;
        V2WorkshopImportCommand = new RelayCommand(WorkshopImportAsync);
        V2LanExportCommand = new RelayCommand(LanExportAsync);
        V2HybridSessionCommand = new RelayCommand(HybridSessionAsync);
        _responsive.StateChanged += (_, _) => OnPropertyChanged(nameof(GridColumns));
        _progressHub.ProgressPublished += OnProgressPublished;
    }
}
