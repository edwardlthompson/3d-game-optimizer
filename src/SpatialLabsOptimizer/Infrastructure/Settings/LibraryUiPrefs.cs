namespace SpatialLabsOptimizer.Infrastructure.Settings;

public sealed record LibraryUiPrefs(
    int SchemaVersion = 1,
    string SortMode = "GameRank",
    string SmartCollection = "None",
    bool ShowFavoritesOnly = false,
    bool ShowLocalOnly = false,
    bool ShowWhyNotReady = false,
    bool FilterUltraNative = false,
    bool FilterTrueGame = false,
    bool FilterUevr = false,
    bool Filter3DVision = false,
    int MinRank3DScore = 0,
    string LastPlaylistName = "");
