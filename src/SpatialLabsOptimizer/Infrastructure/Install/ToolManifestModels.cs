namespace SpatialLabsOptimizer.Infrastructure.Install;

public sealed class ToolManifestDocument
{
    public string Version { get; set; } = "";
    public List<ToolManifestEntryDto> Tools { get; set; } = [];
}

public sealed class ToolManifestEntryDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public string SilentArgs { get; set; } = "";
    public string InstallMode { get; set; } = "";
    public string? ManualInstallGuide { get; set; }
    public string? VendorUrl { get; set; }
    public string? BundledPackage { get; set; }
    public List<int> SuccessExitCodes { get; set; } = [0];
    public string? PostInstallConfig { get; set; }
    public ToolVerificationDto? Verification { get; set; }

    public bool IsManualOnly =>
        string.Equals(InstallMode, "manual", StringComparison.OrdinalIgnoreCase) ||
        (string.IsNullOrWhiteSpace(DownloadUrl) && string.IsNullOrWhiteSpace(BundledPackage));

    public Domain.ToolManifestEntry ToEntry() => new(
        Id,
        Name,
        DownloadUrl,
        Sha256,
        SilentArgs,
        SuccessExitCodes,
        PostInstallConfig,
        BundledPackage);
}

public sealed class ToolVerificationDto
{
    public string Type { get; set; } = "";
    public string? PathHint { get; set; }
}
