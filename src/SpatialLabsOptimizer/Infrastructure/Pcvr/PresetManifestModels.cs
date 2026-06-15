namespace SpatialLabsOptimizer.Infrastructure.Pcvr;

internal sealed class PresetManifestDocument
{
    public List<PresetProfileEntry>? UevrProfiles { get; set; }
}

internal sealed class PresetProfileEntry
{
    public string Id { get; set; } = "";
    public string Url { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public List<int> SteamAppIds { get; set; } = [];
}
