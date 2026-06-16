namespace SpatialLabsOptimizer.Domain;

public sealed record DisplayProfile(
    string Id,
    string Vendor,
    string Model,
    string MarketingName,
    string Type,
    IReadOnlyList<string> EdidSignatures,
    string RecommendedProfileId,
    IReadOnlyList<string> RequiredToolIds);

public sealed record CatalogGameMetadata(
    string BestLevel,
    IReadOnlyList<string> Platforms,
    IReadOnlyList<string> SourceIds,
    string? TrueGameLabel,
    bool IsNvidia3DVisionLegacy,
    int Rank3DScore = 0,
    string? Rank3DLabel = null);

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
    VrCapability VrCapability = VrCapability.None,
    string? SteamVrLaunchOptions = null,
    CatalogGameMetadata? Catalog = null);

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
    bool IsFavorite,
    bool IsCatalogTitle = false);

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
    bool SafeLaunch,
    string PreferredOutput = "Auto");

public sealed record ToolManifestEntry(
    string Id,
    string Name,
    string DownloadUrl,
    string Sha256,
    string SilentArgs,
    IReadOnlyList<int> SuccessExitCodes,
    string? PostInstallConfig,
    string? BundledPackage = null);

public sealed record LaunchStep(
    string Id,
    string Label,
    bool IsComplete,
    bool IsActive,
    bool IsFailed);
