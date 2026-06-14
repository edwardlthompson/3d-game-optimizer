using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SpatialLabsOptimizer.Application.UseCases;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Displays;
using SpatialLabsOptimizer.Infrastructure.Library;
using SpatialLabsOptimizer.Infrastructure.Performance;
using SpatialLabsOptimizer.Infrastructure.Progress;
using SpatialLabsOptimizer.Infrastructure.Responsive;

namespace SpatialLabsOptimizer.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public sealed class RelayCommand : ICommand
{
    private readonly Func<Task>? _executeAsync;
    private readonly Action? _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public RelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
    {
        _executeAsync = executeAsync;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter)
    {
        if (_executeAsync is not null)
        {
            _ = _executeAsync();
        }
        else
        {
            _execute?.Invoke();
        }
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public sealed class ShellViewModel : ViewModelBase
{
    private readonly OperationProgressHub _progressHub;
    private readonly ResponsiveStateService _responsive;
    private readonly SystemSpecsScanner _specsScanner;
    private readonly LibraryIndexer _indexer;
    private readonly SqliteSettingsStore _settings;

    private string _title = "3D Game Optimizer";
    private string _status = "Ready";
    private string _activityMessage = "";
    private bool _showActivityBar;
    private double _activityProgress;
    private bool _showLaunchOverlay;
    private string _launchGameTitle = "";
    private string _launchStep = "";

    public ShellViewModel(
        OperationProgressHub progressHub,
        ResponsiveStateService responsive,
        SystemSpecsScanner specsScanner,
        LibraryIndexer indexer,
        SqliteSettingsStore settings)
    {
        _progressHub = progressHub;
        _responsive = responsive;
        _specsScanner = specsScanner;
        _indexer = indexer;
        _settings = settings;
        _progressHub.ProgressPublished += OnProgressPublished;
        _responsive.StateChanged += (_, _) => OnPropertyChanged(nameof(CurrentColumns));
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public string ActivityMessage
    {
        get => _activityMessage;
        set => SetProperty(ref _activityMessage, value);
    }

    public bool ShowActivityBar
    {
        get => _showActivityBar;
        set => SetProperty(ref _showActivityBar, value);
    }

    public double ActivityProgress
    {
        get => _activityProgress;
        set => SetProperty(ref _activityProgress, value);
    }

    public bool ShowLaunchOverlay
    {
        get => _showLaunchOverlay;
        set => SetProperty(ref _showLaunchOverlay, value);
    }

    public string LaunchGameTitle
    {
        get => _launchGameTitle;
        set => SetProperty(ref _launchGameTitle, value);
    }

    public string LaunchStep
    {
        get => _launchStep;
        set => SetProperty(ref _launchStep, value);
    }

    public int CurrentColumns => _responsive.CurrentColumns;

    public async Task InitializeAsync()
    {
        await _settings.InitializeAsync();
        Status = "Scanning hardware…";
        await _specsScanner.ScanAsync();
        Status = "Indexing library…";
        await _indexer.IndexAsync();
        Status = "Ready";
    }

    private void OnProgressPublished(object? sender, OperationProgressReport report)
    {
        if (report.Category == Application.Progress.OperationCategory.Launch)
        {
            ShowLaunchOverlay = !report.IsComplete && !report.IsFailed;
            LaunchGameTitle = report.Title;
            LaunchStep = report.CurrentStep;
        }
        else
        {
            ShowActivityBar = !report.IsComplete;
            ActivityMessage = $"{report.Title}: {report.CurrentStep}";
            ActivityProgress = report.PercentComplete ?? 0;
        }

        if (report.IsComplete)
        {
            Status = report.CurrentStep;
        }
    }
}

public sealed class GameLibraryViewModel : ViewModelBase
{
    private readonly GameDatabase _database;
    private readonly PlayIn3D _playIn3D;
    private readonly LibrarySortService _sortService;
    private readonly ResponsiveStateService _responsive;
    private readonly ShellViewModel _shell;

    private IReadOnlyList<GameLibraryItemViewModel> _games = Array.Empty<GameLibraryItemViewModel>();
    private LibrarySortMode _sortMode = LibrarySortMode.Quality;
    private string _warmStartStatus = "";

    public GameLibraryViewModel(
        GameDatabase database,
        PlayIn3D playIn3D,
        LibrarySortService sortService,
        ResponsiveStateService responsive,
        ShellViewModel shell)
    {
        _database = database;
        _playIn3D = playIn3D;
        _sortService = sortService;
        _responsive = responsive;
        _shell = shell;
        PlayCommand = new RelayCommand(async () => await PlaySelectedAsync());
        RefreshCommand = new RelayCommand(async () => await LoadAsync());
        _responsive.StateChanged += (_, _) => OnPropertyChanged(nameof(GridColumns));
    }

    public IReadOnlyList<GameLibraryItemViewModel> Games
    {
        get => _games;
        set => SetProperty(ref _games, value);
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

    public string WarmStartStatus
    {
        get => _warmStartStatus;
        set => SetProperty(ref _warmStartStatus, value);
    }

    public int GridColumns => _responsive.CurrentColumns;

    public GameLibraryItemViewModel? SelectedGame { get; set; }

    public ICommand PlayCommand { get; }
    public ICommand RefreshCommand { get; }

    public async Task LoadAsync()
    {
        await _database.InitializeAsync();
        var cached = await _database.GetReadyToPlayAsync();
        WarmStartStatus = cached.Count > 0
            ? $"Warm start — {cached.Count} titles from cache"
            : "Building library index…";
        Games = MapAndSort(cached);
    }

    private void ApplySort()
    {
        if (Games.Count == 0)
        {
            return;
        }

        var sorted = _sortService.Sort(Games.Select(g => g.Source).ToList(), SortMode);
        Games = sorted.Select(g => new GameLibraryItemViewModel(g)).ToList();
    }

    private IReadOnlyList<GameLibraryItemViewModel> MapAndSort(IReadOnlyList<Domain.GameCatalogItem> items)
    {
        var sorted = _sortService.Sort(items, SortMode);
        return sorted.Select(g => new GameLibraryItemViewModel(g)).ToList();
    }

    private async Task PlaySelectedAsync()
    {
        if (SelectedGame is null)
        {
            return;
        }

        _shell.ShowLaunchOverlay = true;
        _shell.LaunchGameTitle = SelectedGame.Title;
        await _playIn3D.ExecuteAsync(SelectedGame.SteamAppId);
        _shell.ShowLaunchOverlay = false;
    }
}

public sealed class SetupWizardViewModel : ViewModelBase
{
    private readonly RunSilentSetup _setup;
    private readonly BenchmarkService _benchmark;
    private readonly DisplayAutoDetector _detector;
    private readonly SqliteSettingsStore _settings;
    private readonly MuxGpuDetector _muxDetector;
    private readonly ViewingDistanceCoach _distanceCoach;

    private int _currentStep;
    private bool _disclaimerAccepted;
    private string _status = "Welcome to 3D Game Optimizer";
    private string _muxWarning = "";
    private string _viewingDistanceTip = "";

    public SetupWizardViewModel(
        RunSilentSetup setup,
        BenchmarkService benchmark,
        DisplayAutoDetector detector,
        SqliteSettingsStore settings,
        MuxGpuDetector muxDetector,
        ViewingDistanceCoach distanceCoach)
    {
        _setup = setup;
        _benchmark = benchmark;
        _detector = detector;
        _settings = settings;
        _muxDetector = muxDetector;
        _distanceCoach = distanceCoach;
        NextCommand = new RelayCommand(async () => await NextAsync(), () => CanProceed);
        RunBenchmarkCommand = new RelayCommand(async () => await _benchmark.RunBenchmarkAsync());
    }

    public string MuxWarning
    {
        get => _muxWarning;
        set => SetProperty(ref _muxWarning, value);
    }

    public string ViewingDistanceTip
    {
        get => _viewingDistanceTip;
        set => SetProperty(ref _viewingDistanceTip, value);
    }

    public int CurrentStep
    {
        get => _currentStep;
        set => SetProperty(ref _currentStep, value);
    }

    public bool DisclaimerAccepted
    {
        get => _disclaimerAccepted;
        set
        {
            if (SetProperty(ref _disclaimerAccepted, value))
            {
                ((RelayCommand)NextCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public IReadOnlyList<Domain.DisplayProfile> DisplayCatalog { get; private set; } = [];

    public Domain.DisplayProfile? SelectedDisplay { get; set; }

    public ICommand NextCommand { get; }
    public ICommand RunBenchmarkCommand { get; }

    public bool CanProceed => CurrentStep switch
    {
        0 => DisclaimerAccepted,
        _ => true
    };

    public async Task LoadAsync()
    {
        await _settings.InitializeAsync();
        DisplayCatalog = await _detector.GetCatalogAsync();
        DisclaimerAccepted = await _settings.GetDisclaimerAcceptedAsync();
        var mux = await _muxDetector.DetectAsync();
        MuxWarning = mux.WarningMessage ?? "";
    }

    private async Task NextAsync()
    {
        switch (CurrentStep)
        {
            case 0:
                await _settings.SetDisclaimerAcceptedAsync(true);
                CurrentStep = 1;
                Status = "Detecting display…";
                SelectedDisplay ??= await _detector.DetectAsync();
                if (SelectedDisplay is not null)
                {
                    ViewingDistanceTip = _distanceCoach.GetTipForProfile(SelectedDisplay.Id);
                }
                break;
            case 1:
                CurrentStep = 2;
                Status = "Running silent install…";
                await _setup.ExecuteAsync(SelectedDisplay);
                break;
            case 2:
                CurrentStep = 3;
                Status = "Setup complete";
                break;
        }
    }
}
