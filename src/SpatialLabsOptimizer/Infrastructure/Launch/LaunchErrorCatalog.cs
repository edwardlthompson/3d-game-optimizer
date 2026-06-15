namespace SpatialLabsOptimizer.Infrastructure.Launch;

public sealed class LaunchErrorCatalog
{
    private readonly Dictionary<string, (string Message, string Recovery)> _errors = new()
    {
        ["3DGO-0001"] = ("Preset cache failed", "Retry preset download"),
        ["3DGO-0002"] = ("Toolchain missing", "Re-run setup wizard"),
        ["3DGO-0003"] = ("Launch blocked — unsupported tier", "View compatibility notes"),
        ["3DGO-0004"] = ("External tool conflict detected", "Enable coexistence or use Safe launch"),
        ["3DGO-0005"] = ("Config apply failed", "Rollback and retry")
    };

    public (string Message, string Recovery) Get(string code) =>
        _errors.TryGetValue(code, out var entry) ? entry : ("Unknown error", "Open logs");
}
