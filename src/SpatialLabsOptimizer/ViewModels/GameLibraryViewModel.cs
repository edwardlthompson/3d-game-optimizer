using System.Windows.Input;
using SpatialLabsOptimizer.Application.UseCases;
using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Launch;
using SpatialLabsOptimizer.Infrastructure.Library;
using SpatialLabsOptimizer.Infrastructure.Responsive;
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

public sealed class GameLibraryViewModel : ViewModelBase
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

    private IReadOnlyList<GameLibraryItemViewModel> _games = Array.Empty<GameLibraryItemViewModel>();
    private IReadOnlyList<string> _playlistNames = Array.Empty<string>();
    private IReadOnlyList<RecentLaunchItemViewModel> _recentLaunches = Array.Empty<RecentLaunchItemViewModel>();
    private LibrarySortMode _sortMode = LibrarySortMode.Quality;
    private SmartCollectionMode _smartCollection = SmartCollectionMode.None;
    private string _warmStartStatus = "";
    private string _preferredOutput = "Auto";
    private string _playlistName = "";
    private string _compatibilityNote = "";
    private string _selectedPresetFreshness = "";
    private string _whyNotReadyHint = "";
    private bool _showFavoritesOnly;
    private bool _showLocalOnly;
    private bool _showWhyNotReady;

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
        IncrementalSteamScanService? incrementalScan = null,
        HdrWatchdogService? hdrWatchdog = null)
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
        _incrementalScan = incrementalScan;
        _hdrWatchdog = hdrWatchdog;

        PlayCommand = new RelayCommand(async () => await PlaySelectedAsync());
        PlayVrCommand = new RelayCommand(async () => await PlayVrSelectedAsync());
        RefreshCommand = new RelayCommand(async () => await LoadAsync());
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
        _responsive.StateChanged += (_, _) => OnPropertyChanged(nameof(GridColumns));
    }

    public event EventHandler? LibraryUpdated;

    public IReadOnlyList<GameLibraryItemViewModel> Games
    {
        get => _games;
        set => SetProperty(ref _games, value);
    }

    public IReadOnlyList<string> PlaylistNames
    {
        get => _playlistNames;
        set => SetProperty(ref _playlistNames, value);
    }

    public IReadOnlyList<RecentLaunchItemViewModel> RecentLaunches
    {
        get => _recentLaunches;
        set => SetProperty(ref _recentLaunches, value);
    }

    public LibrarySortMode SortMode
    {
        get => _sortMode;
        set
        {
            if (SetProperty(ref _sortMode, value))
            {
                ApplySort();
            }
        }
    }

    public SmartCollectionMode SmartCollection
    {
        get => _smartCollection;
        set
        {
            if (SetProperty(ref _smartCollection, value))
            {
                _ = LoadAsync();
            }
        }
    }

    public string WarmStartStatus
    {
        get => _warmStartStatus;
        set => SetProperty(ref _warmStartStatus, value);
    }

    public string PreferredOutput
    {
        get => _preferredOutput;
        set => SetProperty(ref _preferredOutput, value);
    }

    public string PlaylistName
    {
        get => _playlistName;
        set => SetProperty(ref _playlistName, value);
    }

    public string CompatibilityNote
    {
        get => _compatibilityNote;
        set => SetProperty(ref _compatibilityNote, value);
    }

    public string SelectedPresetFreshness
    {
        get => _selectedPresetFreshness;
        set => SetProperty(ref _selectedPresetFreshness, value);
    }

    public string WhyNotReadyHint
    {
        get => _whyNotReadyHint;
        set => SetProperty(ref _whyNotReadyHint, value);
    }

    public bool ShowFavoritesOnly
    {
        get => _showFavoritesOnly;
        set
        {
            if (SetProperty(ref _showFavoritesOnly, value))
            {
                _ = LoadAsync();
            }
        }
    }

    public bool ShowLocalOnly
    {
        get => _showLocalOnly;
        set
        {
            if (SetProperty(ref _showLocalOnly, value))
            {
                _ = LoadAsync();
            }
        }
    }

    public bool ShowWhyNotReady
    {
        get => _showWhyNotReady;
        set
        {
            if (SetProperty(ref _showWhyNotReady, value))
            {
                _ = LoadAsync();
            }
        }
    }

    public int GridColumns => _responsive.CurrentColumns;

    public GameLibraryItemViewModel? SelectedGame { get; set; }

    public ICommand PlayCommand { get; }
    public ICommand PlayVrCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand PinCommand { get; }
    public ICommand UnpinCommand { get; }
    public ICommand QueueCommand { get; }
    public ICommand PlayNextCommand { get; }
    public ICommand ToggleFavoriteCommand { get; }
    public ICommand SavePlaylistCommand { get; }
    public ICommand LoadPlaylistCommand { get; }
    public ICommand SaveOutputCommand { get; }
    public ICommand SaveCompatibilityNoteCommand { get; }
    public ICommand RefreshPresetCommand { get; }

    public async Task LoadAsync()
    {
        if (_hdrWatchdog is not null && await _hdrWatchdog.IsHdrEnabledAsync())
        {
            await _hdrWatchdog.DisableHdrFor3DAsync();
        }

        if (_incrementalScan is not null)
        {
            await _incrementalScan.ScanNewGamesAsync();
        }

        await _database.InitializeAsync();
        var cached = ShowWhyNotReady
            ? await _database.GetAllGamesAsync()
            : await _database.GetReadyToPlayAsync();
        cached = ApplyFilters(cached);

        var pinned = await _pinnedShelf.GetPinnedAppIdsAsync();
        WarmStartStatus = cached.Count > 0
            ? $"Warm start — {cached.Count} titles ({pinned.Count} pinned, queue {_playQueue.Count})"
            : ShowWhyNotReady
                ? "No blocked titles in ready shelf — check full library index."
                : ShowLocalOnly
                    ? "Add a folder in Library Settings to find local installs."
                    : "Building library index…";

        Games = await MapAndSortAsync(cached, pinned);
        PlaylistNames = await _playlists.ListPlaylistNamesAsync();
        RecentLaunches = (await _intelligence.GetRecentLaunchesAsync())
            .Select(e => new RecentLaunchItemViewModel(e))
            .ToList();

        if (SelectedGame is not null)
        {
            var existing = await _overrides.GetAsync(SelectedGame.SteamAppId);
            PreferredOutput = existing?.PreferredOutput ?? "Auto";
            CompatibilityNote = await _intelligence.GetCompatibilityNoteAsync(SelectedGame.SteamAppId) ?? "";
            SelectedPresetFreshness = await _intelligence.GetPresetFreshnessLabelAsync(SelectedGame.SteamAppId);
            WhyNotReadyHint = BuildWhyNotReadyHint(SelectedGame.Source);
        }

        LibraryUpdated?.Invoke(this, EventArgs.Empty);
    }

    public async Task SelectGameAsync(GameLibraryItemViewModel item)
    {
        SelectedGame = item;
        var existing = await _overrides.GetAsync(item.SteamAppId);
        PreferredOutput = existing?.PreferredOutput ?? "Auto";
        CompatibilityNote = await _intelligence.GetCompatibilityNoteAsync(item.SteamAppId) ?? "";
        SelectedPresetFreshness = await _intelligence.GetPresetFreshnessLabelAsync(item.SteamAppId);
        WhyNotReadyHint = BuildWhyNotReadyHint(item.Source);
    }

    public async Task SaveCompatibilityNoteAsync()
    {
        if (SelectedGame is null)
        {
            return;
        }

        await _intelligence.SaveCompatibilityNoteAsync(SelectedGame.SteamAppId, CompatibilityNote);
    }

    public async Task RefreshSelectedPresetAsync()
    {
        if (SelectedGame is null)
        {
            return;
        }

        await _presets.CachePresetAsync(SelectedGame.SteamAppId);
        SelectedPresetFreshness = await _intelligence.GetPresetFreshnessLabelAsync(SelectedGame.SteamAppId);
    }

    public async Task PinSelectedAsync()
    {
        if (SelectedGame is null)
        {
            return;
        }

        var pinned = (await _pinnedShelf.GetPinnedAppIdsAsync()).ToList();
        if (!pinned.Contains(SelectedGame.SteamAppId))
        {
            pinned.Add(SelectedGame.SteamAppId);
            await _pinnedShelf.SetPinnedAppIdsAsync(pinned);
            await LoadAsync();
        }
    }

    public async Task UnpinSelectedAsync()
    {
        if (SelectedGame is null)
        {
            return;
        }

        await _pinnedShelf.RemovePinnedAppIdAsync(SelectedGame.SteamAppId);
        await LoadAsync();
    }

    public async Task EnqueueSelectedAsync()
    {
        if (SelectedGame is null)
        {
            return;
        }

        _playQueue.Enqueue(SelectedGame.SteamAppId);
        await LoadAsync();
    }

    public async Task PlayNextAsync()
    {
        if (!_playQueue.TryDequeue(out var appId))
        {
            return;
        }

        var title = Games.FirstOrDefault(g => g.SteamAppId == appId)?.Title;
        if (title is null)
        {
            var game = await _database.GetGameAsync(appId);
            title = game?.Title ?? $"App {appId}";
        }

        _shell.ShowLaunchOverlay = true;
        _shell.LaunchGameTitle = title;
        await _playIn3D.ExecuteAsync(appId);
        _shell.ShowLaunchOverlay = false;
        await LoadAsync();
    }

    public async Task ToggleFavoriteSelectedAsync()
    {
        if (SelectedGame is null)
        {
            return;
        }

        await _database.SetFavoriteAsync(SelectedGame.SteamAppId, !SelectedGame.IsFavorite);
        await LoadAsync();
    }

    public async Task SavePlaylistAsync()
    {
        if (string.IsNullOrWhiteSpace(PlaylistName) || Games.Count == 0)
        {
            return;
        }

        var ids = Games.Select(g => g.SteamAppId).ToList();
        await _playlists.SavePlaylistAsync(PlaylistName.Trim(), ids);
        await LoadAsync();
    }

    public async Task LoadPlaylistAsync()
    {
        if (string.IsNullOrWhiteSpace(PlaylistName))
        {
            return;
        }

        var ids = await _playlists.LoadPlaylistAsync(PlaylistName.Trim());
        foreach (var id in ids)
        {
            _playQueue.Enqueue(id);
        }

        await LoadAsync();
    }

    public async Task SavePreferredOutputAsync()
    {
        if (SelectedGame is null)
        {
            return;
        }

        var existing = await _overrides.GetAsync(SelectedGame.SteamAppId);
        await _overrides.SaveAsync(new GameOverride(
            SelectedGame.SteamAppId,
            existing?.Depth ?? 0.65,
            existing?.Convergence ?? 0.5,
            existing?.PlatformOverride ?? LaunchPlatform.Uevr,
            existing?.SafeLaunch ?? false,
            PreferredOutput));
    }

    private IReadOnlyList<GameCatalogItem> ApplyFilters(IReadOnlyList<GameCatalogItem> items)
    {
        var filtered = items.AsEnumerable();
        if (ShowFavoritesOnly)
        {
            filtered = filtered.Where(g => g.IsFavorite);
        }

        if (ShowLocalOnly)
        {
            filtered = filtered.Where(g => string.Equals(g.ReviewDescriptor, "Local", StringComparison.OrdinalIgnoreCase));
        }

        if (ShowWhyNotReady)
        {
            filtered = _intelligence.ApplyWhyNotReadyFilter(filtered.ToList());
        }

        if (SmartCollection != SmartCollectionMode.None)
        {
            filtered = _intelligence.ApplySmartCollection(filtered.ToList(), SmartCollection);
        }

        return filtered.ToList();
    }

    private static string BuildWhyNotReadyHint(GameCatalogItem item) => item.Readiness switch
    {
        LaunchReadinessState.NeedsInstall => "Install the game or add its folder under Library Settings.",
        LaunchReadinessState.NeedsPresetCache => "Cache a preset using Refresh preset or run setup bulk cache.",
        LaunchReadinessState.NeedsToolchain => "Complete the setup wizard to install required 3D tools.",
        LaunchReadinessState.Blocked => "Compatibility tier blocks launch — review notes or try Safe launch.",
        _ => "Ready to play in 3D."
    };

    private void ApplySort()
    {
        if (Games.Count == 0)
        {
            return;
        }

        var pinned = Games.Where(g => g.IsPinned).Select(g => g.SteamAppId).ToHashSet();
        var sorted = _sortService.Sort(Games.Select(g => g.Source).ToList(), SortMode);
        Games = sorted.Select(g => new GameLibraryItemViewModel(
            g,
            pinned.Contains(g.SteamAppId),
            LibraryIntelligenceService.GetCompatibilityBadge(
                g.Tier,
                g.Readiness,
                string.Equals(g.ReviewDescriptor, "Local", StringComparison.OrdinalIgnoreCase)),
            null)).ToList();
        LibraryUpdated?.Invoke(this, EventArgs.Empty);
    }

    private async Task<IReadOnlyList<GameLibraryItemViewModel>> MapAndSortAsync(
        IReadOnlyList<GameCatalogItem> items,
        IReadOnlyList<int> pinnedIds)
    {
        var pinned = pinnedIds.ToHashSet();
        var sorted = _sortService.Sort(items, SortMode);
        var viewModels = new List<GameLibraryItemViewModel>();
        foreach (var g in sorted)
        {
            var freshness = await _intelligence.GetPresetFreshnessLabelAsync(g.SteamAppId);
            viewModels.Add(new GameLibraryItemViewModel(
                g,
                pinned.Contains(g.SteamAppId),
                LibraryIntelligenceService.GetCompatibilityBadge(
                    g.Tier,
                    g.Readiness,
                    string.Equals(g.ReviewDescriptor, "Local", StringComparison.OrdinalIgnoreCase)),
                freshness));
        }

        return viewModels;
    }

    private async Task PlaySelectedAsync()
    {
        if (SelectedGame is null)
        {
            return;
        }

        await PlayByAppIdAsync(SelectedGame.SteamAppId, SelectedGame.Title);
    }

    public async Task PlayByAppIdAsync(int appId, string? title = null)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            var game = await _database.GetGameAsync(appId);
            title = game?.Title ?? $"App {appId}";
        }

        _shell.ShowLaunchOverlay = true;
        _shell.LaunchGameTitle = title;
        await _playIn3D.ExecuteAsync(appId);
        _shell.ShowLaunchOverlay = false;
        await LoadAsync();
    }

    private async Task PlayVrSelectedAsync()
    {
        if (SelectedGame is null)
        {
            return;
        }

        _shell.ShowLaunchOverlay = true;
        _shell.LaunchGameTitle = SelectedGame.Title;
        await _playInVr.ExecuteAsync(SelectedGame.SteamAppId);
        _shell.ShowLaunchOverlay = false;
    }
}
