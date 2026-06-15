using Microsoft.UI.Xaml.Controls;
using SpatialLabsOptimizer.Domain;

namespace SpatialLabsOptimizer.Views;

public sealed partial class ManualDisplayPickerView : UserControl
{
    private IReadOnlyList<DisplayProfile> _catalog = Array.Empty<DisplayProfile>();

    public ManualDisplayPickerView()
    {
        InitializeComponent();
    }

    public DisplayProfile? SelectedDisplay { get; private set; }

    public event EventHandler? SelectionChanged;

    public void SetCatalog(IReadOnlyList<DisplayProfile> catalog)
    {
        _catalog = catalog;
        DisplayCombo.Items.Clear();
        foreach (var profile in catalog)
        {
            DisplayCombo.Items.Add($"{profile.Vendor} — {profile.MarketingName}");
        }

        if (DisplayCombo.Items.Count > 0)
        {
            DisplayCombo.SelectedIndex = 0;
        }
    }

    public void SelectProfileById(string profileId)
    {
        for (var i = 0; i < _catalog.Count; i++)
        {
            if (string.Equals(_catalog[i].Id, profileId, StringComparison.OrdinalIgnoreCase))
            {
                DisplayCombo.SelectedIndex = i;
                SelectedDisplay = _catalog[i];
                return;
            }
        }
    }

    private void DisplayCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DisplayCombo.SelectedIndex >= 0 && DisplayCombo.SelectedIndex < _catalog.Count)
        {
            SelectedDisplay = _catalog[DisplayCombo.SelectedIndex];
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
