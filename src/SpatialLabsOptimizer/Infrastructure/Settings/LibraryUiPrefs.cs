namespace SpatialLabsOptimizer.Infrastructure.Settings;

public sealed record LibraryUiPrefs(
    int SchemaVersion = 1,
    string SortMode = "Quality",
    string SmartCollection = "None",
    bool ShowFavoritesOnly = false,
    bool ShowLocalOnly = false,
    bool ShowWhyNotReady = false,
    string LastPlaylistName = "");
