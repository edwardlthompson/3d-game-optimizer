using System.Windows.Input;
using SpatialLabsOptimizer.Infrastructure.Settings;
using SpatialLabsOptimizer.Infrastructure.Updates;
using Windows.System;

namespace SpatialLabsOptimizer.ViewModels;

public sealed class AboutViewModel : ViewModelBase
{
    private readonly UserPreferencesService _prefs;
    private readonly InstallArtifactDetector _detector;
    private readonly UpdateScheduler _scheduler;
    private readonly CatalogUpdateScheduler _catalogScheduler;
    private readonly UpdateApplyService _apply;

    private UpdateCheckResult? _lastResult;
    private string _versionText = "";
    private string _installTypeText = "";
    private string _updateStatusText = "No update information yet.";
    private string _catalogStatusText = "Catalog sync is opt-in and disabled by default.";
    private bool _isApplyEnabled;
    private bool _isRetryOpen;
    private bool _showReleaseNotes;
    private bool _isLoading;
    private int _installTypeOverrideIndex;

    public AboutViewModel(
        UserPreferencesService prefs,
        InstallArtifactDetector detector,
        UpdateScheduler scheduler,
        CatalogUpdateScheduler catalogScheduler,
        UpdateApplyService apply)
    {
        _prefs = prefs;
        _detector = detector;
        _scheduler = scheduler;
        _catalogScheduler = catalogScheduler;
        _apply = apply;

        CheckUpdateCommand = new RelayCommand(async () => await CheckForUpdatesAsync());
        CheckCatalogCommand = new RelayCommand(async () => await CheckCatalogAsync());
        ApplyUpdateCommand = new RelayCommand(async () => await ApplyUpdateAsync(), () => _isApplyEnabled);
        RetryUpdateCommand = new RelayCommand(async () => await ApplyUpdateAsync(), () => _lastResult is not null);
        OpenReleaseNotesCommand = new RelayCommand(async () => await OpenReleaseNotesAsync(), () => _showReleaseNotes);
    }

    public string VersionText
    {
        get => _versionText;
        private set => SetProperty(ref _versionText, value);
    }

    public string InstallTypeText
    {
        get => _installTypeText;
        private set => SetProperty(ref _installTypeText, value);
    }

    public string UpdateStatusText
    {
        get => _updateStatusText;
        private set => SetProperty(ref _updateStatusText, value);
    }

    public bool IsApplyEnabled
    {
        get => _isApplyEnabled;
        private set
        {
            if (SetProperty(ref _isApplyEnabled, value))
            {
                ((RelayCommand)ApplyUpdateCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsRetryOpen
    {
        get => _isRetryOpen;
        private set => SetProperty(ref _isRetryOpen, value);
    }

    public bool ShowReleaseNotes
    {
        get => _showReleaseNotes;
        private set
        {
            if (SetProperty(ref _showReleaseNotes, value))
            {
                ((RelayCommand)OpenReleaseNotesCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public UpdateCheckInterval UpdateInterval { get; private set; } = UpdateCheckInterval.Weekly;

    public UpdateCheckInterval CatalogInterval { get; private set; } = UpdateCheckInterval.Off;

    public int CatalogIntervalIndex
    {
        get => CatalogInterval switch
        {
            UpdateCheckInterval.Off => 0,
            UpdateCheckInterval.Startup => 1,
            UpdateCheckInterval.Daily => 2,
            _ => 3
        };
        set
        {
            if (_isLoading)
            {
                return;
            }

            var interval = value switch
            {
                0 => UpdateCheckInterval.Off,
                1 => UpdateCheckInterval.Startup,
                2 => UpdateCheckInterval.Daily,
                _ => UpdateCheckInterval.Weekly
            };
            _ = SetCatalogIntervalAsync(interval);
        }
    }

    public string CatalogStatusText
    {
        get => _catalogStatusText;
        private set => SetProperty(ref _catalogStatusText, value);
    }

    public int UpdateIntervalIndex
    {
        get => UpdateInterval switch
        {
            UpdateCheckInterval.Off => 0,
            UpdateCheckInterval.Startup => 1,
            UpdateCheckInterval.Daily => 2,
            _ => 3
        };
        set
        {
            if (_isLoading)
            {
                return;
            }

            var interval = value switch
            {
                0 => UpdateCheckInterval.Off,
                1 => UpdateCheckInterval.Startup,
                2 => UpdateCheckInterval.Daily,
                _ => UpdateCheckInterval.Weekly
            };
            _ = SetUpdateIntervalAsync(interval);
        }
    }

    public int InstallTypeOverrideIndex
    {
        get => _installTypeOverrideIndex;
        set
        {
            if (_isLoading)
            {
                return;
            }

            _ = ApplyInstallTypeOverrideIndexAsync(value);
        }
    }

    public ICommand CheckUpdateCommand { get; }
    public ICommand CheckCatalogCommand { get; }
    public ICommand ApplyUpdateCommand { get; }
    public ICommand RetryUpdateCommand { get; }
    public ICommand OpenReleaseNotesCommand { get; }

    public async Task LoadAsync()
    {
        _isLoading = true;
        VersionText = $"Version {ProductVersionReader.ReadCurrentVersion()}";
        var artifactType = await _prefs.GetInstallArtifactTypeAsync(_detector);
        InstallTypeText = $"Installed as: {DescribeInstallType(artifactType)}";
        UpdateInterval = await _prefs.GetUpdateCheckIntervalAsync();
        _lastResult = await _prefs.GetCachedUpdateResultAsync();
        IsRetryOpen = await _prefs.GetUpdateRestartPendingAsync();
        RenderUpdateStatus(_lastResult);
        CatalogInterval = await _prefs.GetCatalogCheckIntervalAsync();
        if (_catalogScheduler.LastResult is not null)
        {
            CatalogStatusText = _catalogScheduler.LastResult.Message;
        }

        _installTypeOverrideIndex = 0;
        OnPropertyChanged(nameof(UpdateIntervalIndex));
        OnPropertyChanged(nameof(CatalogIntervalIndex));
        OnPropertyChanged(nameof(InstallTypeOverrideIndex));
        _isLoading = false;
    }

    public async Task SetCatalogIntervalAsync(UpdateCheckInterval interval)
    {
        CatalogInterval = interval;
        OnPropertyChanged(nameof(CatalogIntervalIndex));
        await _prefs.SetCatalogCheckIntervalAsync(interval);
    }

    public async Task CheckCatalogAsync()
    {
        CatalogStatusText = "Checking catalog…";
        var result = await _catalogScheduler.CheckNowAsync();
        CatalogStatusText = result.Message;
    }

    public async Task SetUpdateIntervalAsync(UpdateCheckInterval interval)
    {
        UpdateInterval = interval;
        OnPropertyChanged(nameof(UpdateIntervalIndex));
        await _prefs.SetUpdateCheckIntervalAsync(interval);
    }

    private async Task ApplyInstallTypeOverrideIndexAsync(int index)
    {
        _installTypeOverrideIndex = index;
        if (index == 0)
        {
            await SetInstallTypeOverrideAsync(null);
            return;
        }

        var type = index switch
        {
            1 => InstallArtifactType.Zip,
            _ => InstallArtifactType.Msi
        };
        await SetInstallTypeOverrideAsync(type);
    }

    public async Task SetInstallTypeOverrideAsync(InstallArtifactType? type)
    {
        if (type is null)
        {
            var detected = _detector.Detect();
            await _prefs.SetInstallArtifactTypeAsync(detected);
            InstallTypeText = $"Installed as: {DescribeInstallType(detected)}";
            return;
        }

        await _prefs.SetInstallArtifactTypeAsync(type.Value);
        InstallTypeText = $"Installed as: {DescribeInstallType(type.Value)} (override)";
    }

    public async Task CheckForUpdatesAsync()
    {
        UpdateStatusText = "Checking…";
        _lastResult = await _scheduler.CheckNowAsync();
        RenderUpdateStatus(_lastResult);
    }

    public async Task ApplyUpdateAsync()
    {
        if (_lastResult is null)
        {
            _lastResult = await _prefs.GetCachedUpdateResultAsync();
        }

        if (_lastResult is null)
        {
            UpdateStatusText = "No staged update metadata — check for updates first.";
            return;
        }

        IsApplyEnabled = false;
        UpdateStatusText = "Applying update…";
        try
        {
            await _apply.ApplyUpdateAsync(_lastResult);
        }
        catch (Exception ex)
        {
            UpdateStatusText = ex.Message;
            IsApplyEnabled = _lastResult.IsUpdateAvailable;
        }
    }

    public async Task OpenReleaseNotesAsync()
    {
        if (_lastResult?.ReleasePageUrl is not null)
        {
            await Launcher.LaunchUriAsync(new Uri(_lastResult.ReleasePageUrl));
        }
    }

    private void RenderUpdateStatus(UpdateCheckResult? result)
    {
        if (result is null)
        {
            UpdateStatusText = "No update information yet.";
            IsApplyEnabled = false;
            ShowReleaseNotes = false;
            return;
        }

        if (!string.IsNullOrWhiteSpace(result.ErrorMessage) && result.LatestVersion is null)
        {
            UpdateStatusText = result.ErrorMessage;
            IsApplyEnabled = false;
            ShowReleaseNotes = false;
            return;
        }

        if (result.IsUpdateAvailable)
        {
            UpdateStatusText = string.IsNullOrWhiteSpace(result.ErrorMessage)
                ? $"Update available: v{result.LatestVersion} (you have v{result.CurrentVersion})."
                : $"Update available: v{result.LatestVersion}. {result.ErrorMessage}";
            IsApplyEnabled = !string.IsNullOrWhiteSpace(result.DownloadUrl);
            ShowReleaseNotes = true;
            return;
        }

        UpdateStatusText = $"Up to date (v{result.CurrentVersion}).";
        IsApplyEnabled = false;
        ShowReleaseNotes = false;
    }

    private static string DescribeInstallType(InstallArtifactType type) => type switch
    {
        InstallArtifactType.Msi => "MSI",
        _ => "Portable (zip)"
    };
}
