namespace SpatialLabsOptimizer.Domain;

public sealed record DisplayProfile(
    string Id,
    string Vendor,
    string Model,
    string MarketingName,
    string Type,
    IReadOnlyList<string> EdidSignatures,
    string RecommendedProfileId);

public sealed record GameCompatibilityEntry(
    string Id,
    string Title,
    int SteamAppId,
    IReadOnlyList<string> SteamTags,
    IReadOnlyDictionary<string, string> TiersByVendor,
    string ReviewSummary,
    int? ReviewScorePercent = null,
    int? ReviewCount = null,
    double? ReviewSortScore = null,
    int? PeakPlayers = null,
    int? CurrentPlayers = null,
    string? CoverCachePath = null,
    VrCapability VrCapability = VrCapability.None);

public sealed record GameCatalogItem(
    int SteamAppId,
    string Title,
    CompatibilityTier Tier,
    LaunchReadinessState Readiness,
    bool IsInstalled,
    int? CurrentPlayers,
    int? ReviewScorePercent,
    int? ReviewCount,
    double? ReviewSortScore,
    string? CoverCachePath,
    string? ReviewDescriptor,
    bool IsFavorite);

public sealed record HardwareProfile(
    string CpuName,
    string GpuName,
    int VramMb,
    int RamMb,
    string DisplayName,
    string DriverVersion);

public sealed record ResolvedGameLaunchPlan(
    int SteamAppId,
    string Title,
    LaunchPlatform Platform,
    CompatibilityTier Tier,
    double Depth,
    double Convergence,
    double Separation,
    string? PresetId,
    string? ShaderId,
    bool SafeLaunch);

public sealed record ToolManifestEntry(
    string Id,
    string Name,
    string DownloadUrl,
    string Sha256,
    string SilentArgs,
    IReadOnlyList<int> SuccessExitCodes,
    string? PostInstallConfig);

public sealed record LaunchStep(
    string Id,
    string Label,
    bool IsComplete,
    bool IsActive,
    bool IsFailed);
