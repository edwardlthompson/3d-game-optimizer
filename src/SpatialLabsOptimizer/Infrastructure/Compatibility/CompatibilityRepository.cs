using System.Text.Json;
using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Data;

namespace SpatialLabsOptimizer.Infrastructure.Compatibility;

public sealed class CompatibilityRepository
{
    private readonly JsonDataLoader _loader;
    private IReadOnlyList<GameCompatibilityEntry>? _cache;

    public CompatibilityRepository(JsonDataLoader loader)
    {
        _loader = loader;
    }

    public void InvalidateCache() => _cache = null;

    public async Task<IReadOnlyList<GameCompatibilityEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (_cache is not null)
        {
            return _cache;
        }

        var catalog = await TryLoadCatalogV2Async(cancellationToken);
        if (catalog is not null)
        {
            _cache = catalog;
            return _cache;
        }

        var seed = await _loader.LoadAsync<CompatibilitySeedDocument>("compatibility/seed-v1.json", cancellationToken);
        if (seed?.Games is null)
        {
            _cache = Array.Empty<GameCompatibilityEntry>();
            return _cache;
        }

        _cache = seed.Games.Select(MapSeedGame).ToList();
        return _cache;
    }

    public async Task<GameCompatibilityEntry?> GetByAppIdAsync(int appId, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);
        return all.FirstOrDefault(g => g.SteamAppId == appId);
    }

    public CompatibilityTier MapTier(string tierValue) => tierValue switch
    {
        "optimized" => CompatibilityTier.Optimized,
        "playable" => CompatibilityTier.Playable,
        "experimental" => CompatibilityTier.Experimental,
        _ => CompatibilityTier.Unsupported
    };

    public CompatibilityTier GetTierForVendor(GameCompatibilityEntry entry, DisplayVendor vendor)
    {
        var key = vendor switch
        {
            DisplayVendor.AcerSpatialLabs => "acer",
            DisplayVendor.SamsungOdyssey3D => "samsung",
            DisplayVendor.Nvidia3DVision => "nvidia",
            _ => "generic"
        };

        return entry.TiersByVendor.TryGetValue(key, out var tier)
            ? MapTier(tier)
            : CompatibilityTier.Unsupported;
    }

    private async Task<IReadOnlyList<GameCompatibilityEntry>?> TryLoadCatalogV2Async(CancellationToken cancellationToken)
    {
        var document = await _loader.LoadAsync<CatalogV2Document>("compatibility/catalog-v2.json", cancellationToken);
        if (document?.Games is null || document.Games.Count == 0)
        {
            return null;
        }

        return document.Games
            .Where(g => g.BestLevel != "unsupported2d")
            .Select(MapCatalogGame)
            .ToList();
    }

    private static GameCompatibilityEntry MapSeedGame(SeedGame g) => new(
        g.Id,
        g.Title,
        g.SteamAppId,
        g.SteamTags,
        g.TiersByVendor,
        g.Review.Summary,
        VrCapability: MapVrCapability(g.VrCapability),
        SteamVrLaunchOptions: g.SteamVrLaunchOptions);

    private static GameCompatibilityEntry MapCatalogGame(CatalogV2Game g)
    {
        var tags = g.SteamTags is { Count: > 0 }
            ? g.SteamTags
            : g.SteamStats?.Tags ?? [];

        if (tags.Count == 0)
        {
            tags = ["3D"];
        }

        return new GameCompatibilityEntry(
            g.Id,
            g.Title,
            g.SteamAppId ?? 0,
            tags,
            g.TiersByVendor,
            g.ReviewSummary ?? "Listed in the multi-source 3D catalog.",
            ReviewScorePercent: g.SteamStats?.ReviewPercent,
            ReviewCount: g.SteamStats?.ReviewCount,
            PeakPlayers: g.SteamStats?.PeakPlayers,
            CurrentPlayers: g.SteamStats?.CurrentPlayers,
            VrCapability: MapVrCapability(g.VrCapability),
            SteamVrLaunchOptions: g.SteamVrLaunchOptions);
    }

    private static VrCapability MapVrCapability(string? value) => value switch
    {
        "nativeVr" => VrCapability.NativeVr,
        "uevrCompatible" => VrCapability.UevrCompatible,
        _ => VrCapability.None
    };

    private sealed class CompatibilitySeedDocument
    {
        public string Version { get; set; } = "";
        public List<SeedGame> Games { get; set; } = [];
    }

    private sealed class SeedGame
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public int SteamAppId { get; set; }
        public List<string> SteamTags { get; set; } = [];
        public Dictionary<string, string> TiersByVendor { get; set; } = [];
        public SeedReview Review { get; set; } = new();
        public string? VrCapability { get; set; }
        public string? SteamVrLaunchOptions { get; set; }
    }

    private sealed class SeedReview
    {
        public string Summary { get; set; } = "";
        public string Confidence { get; set; } = "";
        public string LastReviewedAt { get; set; } = "";
    }

    private sealed class CatalogV2Document
    {
        public string Version { get; set; } = "";
        public List<CatalogV2Game> Games { get; set; } = [];
    }

    private sealed class CatalogV2Game
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public int? SteamAppId { get; set; }
        public List<string>? SteamTags { get; set; }
        public string BestLevel { get; set; } = "";
        public Dictionary<string, string> TiersByVendor { get; set; } = [];
        public string? ReviewSummary { get; set; }
        public CatalogSteamStats? SteamStats { get; set; }
        public string? VrCapability { get; set; }
        public string? SteamVrLaunchOptions { get; set; }
    }

    private sealed class CatalogSteamStats
    {
        public int? ReviewPercent { get; set; }
        public int? ReviewCount { get; set; }
        public int? PeakPlayers { get; set; }
        public int? CurrentPlayers { get; set; }
        public List<string>? Tags { get; set; }
    }
}
