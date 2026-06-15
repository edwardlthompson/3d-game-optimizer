using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Data;

namespace SpatialLabsOptimizer.Infrastructure.Performance;

public sealed class PerformanceTierEstimator
{
    private readonly JsonDataLoader _loader;

    private static readonly Dictionary<string, int> DefaultMinVramMb = new(StringComparer.OrdinalIgnoreCase)
    {
        ["enthusiast"] = 16384,
        ["high"] = 8192,
        ["medium"] = 4096,
        ["low"] = 0
    };

    public PerformanceTierEstimator(JsonDataLoader loader)
    {
        _loader = loader;
    }

    public async Task<PerformanceTier> EstimateAsync(HardwareProfile profile, CancellationToken cancellationToken = default)
    {
        var tiers = await _loader.LoadAsync<PerformanceTiersDocument>("performance/performance-tiers-v1.json", cancellationToken);
        var rules = tiers?.Tiers?
            .Select(t => (Id: t.Id, MinVramMb: t.MinVramMb > 0 ? t.MinVramMb : DefaultMinVramMb.GetValueOrDefault(t.Id, 0)))
            .OrderByDescending(t => t.MinVramMb)
            .ToList();

        if (rules is null || rules.Count == 0)
        {
            return profile.VramMb >= 12288 ? PerformanceTier.High : PerformanceTier.Medium;
        }

        foreach (var rule in rules)
        {
            if (profile.VramMb >= rule.MinVramMb)
            {
                return rule.Id.ToLowerInvariant() switch
                {
                    "enthusiast" => PerformanceTier.Enthusiast,
                    "high" => PerformanceTier.High,
                    "medium" => PerformanceTier.Medium,
                    _ => PerformanceTier.Low
                };
            }
        }

        return PerformanceTier.Low;
    }

    private sealed class PerformanceTiersDocument
    {
        public List<TierRule> Tiers { get; set; } = [];
    }

    private sealed class TierRule
    {
        public string Id { get; set; } = "";
        public int MinVramMb { get; set; }
    }
}
