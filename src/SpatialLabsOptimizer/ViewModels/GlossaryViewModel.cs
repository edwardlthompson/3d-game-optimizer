using SpatialLabsOptimizer.Infrastructure.Data;

namespace SpatialLabsOptimizer.ViewModels;

public sealed class GlossaryEntryViewModel
{
    public GlossaryEntryViewModel(string term, string definition)
    {
        Term = term;
        Definition = definition;
    }

    public string Term { get; }
    public string Definition { get; }
}

public sealed class GlossaryViewModel : ViewModelBase
{
    private readonly JsonDataLoader _loader;
    private IReadOnlyList<GlossaryEntryViewModel> _entries = Array.Empty<GlossaryEntryViewModel>();
    private string _loadError = "";

    public GlossaryViewModel(JsonDataLoader loader)
    {
        _loader = loader;
    }

    public IReadOnlyList<GlossaryEntryViewModel> Entries
    {
        get => _entries;
        private set => SetProperty(ref _entries, value);
    }

    public string LoadError
    {
        get => _loadError;
        private set => SetProperty(ref _loadError, value);
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var doc = await _loader.LoadAsync<GlossaryDocument>("glossary/glossary-v1.json", cancellationToken);
            Entries = doc.Entries
                .Where(e => !string.IsNullOrWhiteSpace(e.Term))
                .Select(e => new GlossaryEntryViewModel(e.Term, e.Definition))
                .ToList();
            LoadError = Entries.Count == 0 ? "No glossary entries found in seed data." : "";
        }
        catch (Exception ex)
        {
            Entries = [];
            LoadError = $"Could not load glossary: {ex.Message}";
        }
    }
}
