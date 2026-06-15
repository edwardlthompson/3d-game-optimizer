namespace SpatialLabsOptimizer.Infrastructure.Data;

public sealed record LocalGameInstallRecord(
    int StableAppId,
    string FolderPath,
    string LaunchExe,
    string DisplayTitle,
    DateTimeOffset LastScannedAt,
    bool IsStale);

public sealed record RecentLaunchRow(
    int StableAppId,
    string Title,
    DateTimeOffset LaunchedAt,
    bool Success,
    string? ErrorCode);
