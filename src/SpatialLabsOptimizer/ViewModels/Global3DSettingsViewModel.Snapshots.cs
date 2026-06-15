using SpatialLabsOptimizer.Infrastructure.Launch;

namespace SpatialLabsOptimizer.ViewModels;

public sealed record SnapshotListItemViewModel(int AppId, string Path, string Label);

public sealed partial class Global3DSettingsViewModel
{
    private readonly ConfigSnapshotService _snapshots;

    private string _snapshotFilterAppId = string.Empty;
    private IReadOnlyList<SnapshotListItemViewModel> _snapshotItems = [];
    private SnapshotListItemViewModel? _selectedSnapshot;
    private string _snapshotStatus = string.Empty;

    public string SnapshotFilterAppId
    {
        get => _snapshotFilterAppId;
        set
        {
            if (SetProperty(ref _snapshotFilterAppId, value))
            {
                RefreshSnapshots();
            }
        }
    }

    public IReadOnlyList<SnapshotListItemViewModel> SnapshotItems
    {
        get => _snapshotItems;
        private set => SetProperty(ref _snapshotItems, value);
    }

    public SnapshotListItemViewModel? SelectedSnapshot
    {
        get => _selectedSnapshot;
        set
        {
            if (SetProperty(ref _selectedSnapshot, value))
            {
                OnPropertyChanged(nameof(CanRestoreSnapshot));
                if (RestoreSnapshotCommand is RelayCommand restore)
                {
                    restore.RaiseCanExecuteChanged();
                }
            }
        }
    }

    public string SnapshotStatus
    {
        get => _snapshotStatus;
        private set => SetProperty(ref _snapshotStatus, value);
    }

    public bool CanRestoreSnapshot => SelectedSnapshot is not null;

    private void RefreshSnapshots()
    {
        int? filterAppId = int.TryParse(SnapshotFilterAppId.Trim(), out var appId) && appId > 0
            ? appId
            : null;
        SnapshotItems = _snapshots.ListSnapshots(filterAppId)
            .Select(entry => new SnapshotListItemViewModel(
                entry.AppId,
                entry.Path,
                $"{entry.AppId} — {entry.CreatedAt:yyyy-MM-dd HH:mm}"))
            .ToList();
        OnPropertyChanged(nameof(CanRestoreSnapshot));
    }

    private async Task RestoreSnapshotAsync()
    {
        if (SelectedSnapshot is null)
        {
            return;
        }

        await _snapshots.RollbackAsync(SelectedSnapshot.Path);
        SnapshotStatus = $"Restored snapshot for app {SelectedSnapshot.AppId}.";
        RefreshSnapshots();
    }
}
