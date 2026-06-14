using Microsoft.UI.Xaml.Controls;

namespace SpatialLabsOptimizer.Controls;

public sealed partial class OperationProgressDialog : ContentDialog
{
    public OperationProgressDialog()
    {
        InitializeComponent();
    }

    public void UpdateProgress(string title, string step, double? percent, string? detail)
    {
        TitleBlock.Text = title;
        StepBlock.Text = step;
        DetailBlock.Text = detail ?? "";
        ProgressBar.IsIndeterminate = !percent.HasValue;
        if (percent.HasValue)
        {
            ProgressBar.Value = percent.Value;
        }
    }
}
