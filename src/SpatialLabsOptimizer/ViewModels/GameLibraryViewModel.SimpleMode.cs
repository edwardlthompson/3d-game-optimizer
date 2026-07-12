namespace SpatialLabsOptimizer.ViewModels;

public sealed partial class GameLibraryViewModel
{
    private bool _simpleMode;

    public bool SimpleMode
    {
        get => _simpleMode;
        private set
        {
            if (SetProperty(ref _simpleMode, value))
            {
                OnPropertyChanged(nameof(ShowDenseFilters));
            }
        }
    }

    public bool ShowDenseFilters => !SimpleMode;

    public string ActiveFilterChips
    {
        get
        {
            var chips = new List<string>();
            if (ShowLocalOnly) chips.Add("Local");
            if (ShowWhyNotReady) chips.Add("Not ready");
            if (FilterUltraNative) chips.Add("Ultra/Native");
            if (FilterTrueGame) chips.Add("TrueGame");
            if (FilterUevr) chips.Add("UEVR");
            if (Filter3DVision) chips.Add("3D Vision");
            return chips.Count == 0 ? "" : "Active: " + string.Join(" · ", chips);
        }
    }
}
