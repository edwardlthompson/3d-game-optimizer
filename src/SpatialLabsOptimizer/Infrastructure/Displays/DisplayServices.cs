using System.Text.Json;
using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Data;

namespace SpatialLabsOptimizer.Infrastructure.Displays;

public interface IDisplayVendorAdapter
{
    DisplayVendor Vendor { get; }
    string DisplayProfileId { get; }
    Task<bool> IsHubInstalledAsync(CancellationToken cancellationToken = default);
    Task<bool> InstallHubSilentlyAsync(CancellationToken cancellationToken = default);
    Task ApplyOptimalDefaultsAsync(CancellationToken cancellationToken = default);
    LaunchPlatform GetPreferredLaunchPlatform(CompatibilityTier tier);
}

public sealed class DisplayAutoDetector
{
    private readonly JsonDataLoader _loader;

    public DisplayAutoDetector(JsonDataLoader loader)
    {
        _loader = loader;
    }

    public async Task<IReadOnlyList<DisplayProfile>> GetCatalogAsync(CancellationToken cancellationToken = default)
    {
        var doc = await _loader.LoadAsync<DisplayCatalogDocument>("displays/display-catalog-v1.json", cancellationToken);
        if (doc?.Displays is null)
        {
            return Array.Empty<DisplayProfile>();
        }

        return doc.Displays.Select(d => new DisplayProfile(
            d.Id,
            d.Vendor,
            d.Model,
            d.MarketingName,
            d.Type,
            d.EdidSignatures,
            d.RecommendedProfileId)).ToList();
    }

    public async Task<DisplayProfile?> DetectAsync(CancellationToken cancellationToken = default)
    {
        var catalog = await GetCatalogAsync(cancellationToken);
        // EDID probing requires hardware APIs; return generic manual as fallback for dev.
        return catalog.FirstOrDefault(d => d.Id == "generic-manual")
            ?? catalog.FirstOrDefault();
    }

    public IDisplayVendorAdapter CreateAdapter(DisplayProfile profile) => profile.Id switch
    {
        "acer-psv27-2" or "acer-spatiallabs-15" => new AcerSpatialLabsAdapter(profile),
        "samsung-g90xf" => new SamsungOdyssey3DAdapter(profile),
        "nvidia-3d-vision-generic" => new Nvidia3DVisionAdapter(profile),
        _ => new GenericStereoscopicAdapter(profile)
    };

    private sealed class DisplayCatalogDocument
    {
        public string Version { get; set; } = "";
        public List<DisplayCatalogEntry> Displays { get; set; } = [];
    }

    private sealed class DisplayCatalogEntry
    {
        public string Id { get; set; } = "";
        public string Vendor { get; set; } = "";
        public string Model { get; set; } = "";
        public string MarketingName { get; set; } = "";
        public string Type { get; set; } = "";
        public List<string> EdidSignatures { get; set; } = [];
        public string RecommendedProfileId { get; set; } = "";
    }
}

public abstract class DisplayVendorAdapterBase : IDisplayVendorAdapter
{
    protected DisplayVendorAdapterBase(DisplayProfile profile)
    {
        Profile = profile;
    }

    protected DisplayProfile Profile { get; }

    public abstract DisplayVendor Vendor { get; }
    public string DisplayProfileId => Profile.Id;

    public virtual Task<bool> IsHubInstalledAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public virtual Task<bool> InstallHubSilentlyAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public virtual Task ApplyOptimalDefaultsAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public abstract LaunchPlatform GetPreferredLaunchPlatform(CompatibilityTier tier);
}

public sealed class AcerSpatialLabsAdapter : DisplayVendorAdapterBase
{
    public AcerSpatialLabsAdapter(DisplayProfile profile) : base(profile) { }
    public override DisplayVendor Vendor => DisplayVendor.AcerSpatialLabs;
    public override LaunchPlatform GetPreferredLaunchPlatform(CompatibilityTier tier) =>
        tier <= CompatibilityTier.Optimized ? LaunchPlatform.TrueGame : LaunchPlatform.Uevr;
}

public sealed class SamsungOdyssey3DAdapter : DisplayVendorAdapterBase
{
    public SamsungOdyssey3DAdapter(DisplayProfile profile) : base(profile) { }
    public override DisplayVendor Vendor => DisplayVendor.SamsungOdyssey3D;
    public override LaunchPlatform GetPreferredLaunchPlatform(CompatibilityTier tier) =>
        tier <= CompatibilityTier.Optimized ? LaunchPlatform.Odyssey3DHub : LaunchPlatform.Uevr;
}

public sealed class Nvidia3DVisionAdapter : DisplayVendorAdapterBase
{
    public Nvidia3DVisionAdapter(DisplayProfile profile) : base(profile) { }
    public override DisplayVendor Vendor => DisplayVendor.Nvidia3DVision;
    public override LaunchPlatform GetPreferredLaunchPlatform(CompatibilityTier tier) =>
        tier <= CompatibilityTier.Playable ? LaunchPlatform.Nvidia3DVision : LaunchPlatform.ReShade;
}

public sealed class GenericStereoscopicAdapter : DisplayVendorAdapterBase
{
    public GenericStereoscopicAdapter(DisplayProfile profile) : base(profile) { }
    public override DisplayVendor Vendor => DisplayVendor.Generic;
    public override LaunchPlatform GetPreferredLaunchPlatform(CompatibilityTier tier) =>
        tier switch
        {
            CompatibilityTier.Optimized or CompatibilityTier.Native => LaunchPlatform.Uevr,
            CompatibilityTier.Playable => LaunchPlatform.ReShade,
            CompatibilityTier.Experimental => LaunchPlatform.Tweak,
            _ => LaunchPlatform.Blocked
        };
}
