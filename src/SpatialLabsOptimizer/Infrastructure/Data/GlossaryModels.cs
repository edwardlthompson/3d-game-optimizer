namespace SpatialLabsOptimizer.Infrastructure.Data;

public sealed class GlossaryDocument
{
    public int Version { get; init; }
    public List<GlossaryEntryDocument> Entries { get; init; } = [];
}

public sealed class GlossaryEntryDocument
{
    public string Term { get; init; } = "";
    public string Definition { get; init; } = "";
}
