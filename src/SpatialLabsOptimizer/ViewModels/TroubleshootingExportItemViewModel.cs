namespace SpatialLabsOptimizer.ViewModels;

public sealed class TroubleshootingExportItemViewModel : ViewModelBase
{
    private bool _isSelected = true;

    public TroubleshootingExportItemViewModel(int appId, string title)
    {
        AppId = appId;
        Title = title;
    }

    public int AppId { get; }
    public string Title { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
