using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Compatibility;
using SpatialLabsOptimizer.Infrastructure.Library;
using SpatialLabsOptimizer.Infrastructure.Privacy;

namespace SpatialLabsOptimizer.Tests;

public class CatalogFeatureTests
{
    [Fact]
    public void PrivacyAllowlist_IncludesGitHubPagesCatalogHost()
    {
        Assert.Contains("edwardlthompson.github.io", PrivacyAllowlist.DefaultHosts);
    }

    [Fact]
    public void CatalogFilterHelper_MatchesUltraNative()
    {
        var catalog = new CatalogGameMetadata("ultra3d", ["truegame"], ["acer-truegame"], "3D Ultra", false);
        Assert.True(CatalogFilterHelper.MatchesUltraNative(catalog));
        Assert.True(CatalogFilterHelper.MatchesTrueGame(catalog));
    }

    [Fact]
    public void CatalogFilterHelper_BuildsSourceBadges()
    {
        var catalog = new CatalogGameMetadata(
            "native3d",
            ["nvidia-3d-vision"],
            ["nvidia-3d-vision"],
            null,
            true);
        var badges = CatalogFilterHelper.BuildSourceBadges(catalog);
        Assert.Contains("3D", badges);
        Assert.Contains("3D Vision (legacy)", badges);
    }

    [Fact]
    public void LibraryIntelligence_NeedsPresetCacheBadge_IsNeedsPreset()
    {
        var badge = LibraryIntelligenceService.GetCompatibilityBadge(
            CompatibilityTier.Playable,
            LaunchReadinessState.NeedsPresetCache,
            isLocal: false);
        Assert.Equal("Needs preset", badge);
    }

    [Fact]
    public void CatalogToolHints_RecommendsUevrAndReshade()
    {
        var catalog = new CatalogGameMetadata(
            "optimized3d",
            ["uevr"],
            ["uevr-profiles"],
            null,
            false);
        var tools = CatalogToolHints.GetRecommendedToolIds(catalog);
        Assert.Contains("uevr", tools);
        Assert.Contains("reshade", tools);
    }
}
