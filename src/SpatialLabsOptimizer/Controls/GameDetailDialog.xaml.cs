using Microsoft.UI.Xaml.Controls;
using SpatialLabsOptimizer.ViewModels;

namespace SpatialLabsOptimizer.Controls;

public sealed partial class GameDetailDialog : ContentDialog
{
    public GameLibraryViewModel ViewModel { get; }

    public GameDetailDialog(GameLibraryViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }
}
