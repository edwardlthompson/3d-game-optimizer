using SpatialLabsOptimizer.Infrastructure.Displays;

namespace SpatialLabsOptimizer.Infrastructure.Library;

public sealed class LibraryIndexMerger
{
    private readonly LibraryExternalGamesMerger _external;
    private readonly LibrarySteamOwnedMerger _steamOwned;
    private readonly LibraryStorePlaceholderAssigner _placeholders;

    public LibraryIndexMerger(
        LibraryExternalGamesMerger external,
        LibrarySteamOwnedMerger steamOwned,
        LibraryStorePlaceholderAssigner placeholders)
    {
        _external = external;
        _steamOwned = steamOwned;
        _placeholders = placeholders;
    }

    public async Task MergeExternalStoresAsync(
        HashSet<int> steamInstalled,
        IDisplayVendorAdapter? adapter,
        CancellationToken cancellationToken)
    {
        await _external.MergeAsync(steamInstalled, adapter, cancellationToken);
        await _steamOwned.MergeOwnedGamesAsync(steamInstalled, adapter, cancellationToken);
        await _placeholders.AssignAsync(cancellationToken);
    }
}
