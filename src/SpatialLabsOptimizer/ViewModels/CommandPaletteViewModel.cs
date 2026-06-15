using SpatialLabsOptimizer.Infrastructure.Pcvr;

namespace SpatialLabsOptimizer.ViewModels;

public sealed class CommandPaletteViewModel : ViewModelBase
{
    private readonly CommandPaletteService _palette;
    private IReadOnlyList<CommandPaletteEntry> _results = Array.Empty<CommandPaletteEntry>();

    public CommandPaletteViewModel(CommandPaletteService palette)
    {
        _palette = palette;
    }

    public IReadOnlyList<CommandPaletteEntry> Results
    {
        get => _results;
        private set => SetProperty(ref _results, value);
    }

    public void Search(string query) => Results = _palette.Search(query);
}
