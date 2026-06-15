namespace SpatialLabsOptimizer.Infrastructure.Install;

public sealed class InstallErrorCatalog
{
    private readonly Dictionary<int, (string Code, string Message, string Recovery)> _errors = new()
    {
        [1] = ("3DGO-0101", "Tool ID not allowlisted", "Verify tool manifest entry"),
        [2] = ("3DGO-0102", "Download URL not allowlisted", "Check vendor download source"),
        [3] = ("3DGO-0103", "Installer hash mismatch", "Re-download toolchain package"),
        [4] = ("3DGO-0106", "Download URL missing", "Add downloadUrl to tool manifest or install manually"),
        [5] = ("3DGO-0107", "Download failed", "Check network and allowlisted source"),
        [6] = ("3DGO-0108", "SHA256 required", "Add sha256 hash to tool manifest entry"),
        [-1] = ("3DGO-0104", "Elevated helper missing", "Reinstall SpatialLabs Optimizer"),
        [-2] = ("3DGO-0105", "Silent install failed", "Re-run setup wizard or install manually")
    };

    public (string Code, string Message, string Recovery) Classify(int exitCode, bool helperMissing)
    {
        if (helperMissing)
        {
            return _errors[-1];
        }

        if (_errors.TryGetValue(exitCode, out var entry))
        {
            return entry;
        }

        return exitCode == 0
            ? ("3DGO-0000", "Install succeeded", "")
            : _errors[-2];
    }
}
