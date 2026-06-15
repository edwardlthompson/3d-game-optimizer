using SpatialLabsOptimizer.Infrastructure.Data;

namespace SpatialLabsOptimizer.Infrastructure.Install;

public sealed class ToolConfigWriter
{
    public Task ApplyReShadeConfigAsync(string gamePath, double depth, double convergence, CancellationToken cancellationToken = default)
        => WriteIniAsync(Path.Combine(gamePath, "ReShade.ini"), depth, convergence, cancellationToken);

    public Task ApplyUevrConfigAsync(string profilePath, double depth, CancellationToken cancellationToken = default)
        => WriteIniAsync(profilePath, depth, 0.5, cancellationToken);

    private static async Task WriteIniAsync(string path, double depth, double convergence, CancellationToken cancellationToken)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var content = $"""
            [3D]
            Depth={depth:F2}
            Convergence={convergence:F2}
            """;
        await File.WriteAllTextAsync(path, content, cancellationToken);
    }
}

public sealed class OptimalDefaultsService
{
    private readonly JsonDataLoader _loader;
    private readonly ToolConfigWriter _configWriter;

    public OptimalDefaultsService(JsonDataLoader loader, ToolConfigWriter configWriter)
    {
        _loader = loader;
        _configWriter = configWriter;
    }

    public async Task ApplyForProfileAsync(string profileId, CancellationToken cancellationToken = default)
    {
        var defaults = await _loader.LoadAsync<OptimalDefaultsDocument>("defaults/optimal-displays-v1.json", cancellationToken);
        var entry = defaults?.Profiles?.FirstOrDefault(p =>
            string.Equals(p.Id, profileId, StringComparison.OrdinalIgnoreCase));
        if (entry?.Defaults is null)
        {
            return;
        }

        var configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "3d-game-optimizer", "config", "global-3d.ini");
        await _configWriter.ApplyReShadeConfigAsync(
            Path.GetDirectoryName(configPath) ?? "",
            entry.Defaults.DepthScale,
            entry.Defaults.ConvergenceOffset,
            cancellationToken);
    }

    private sealed class OptimalDefaultsDocument
    {
        public string Version { get; set; } = "";
        public List<OptimalProfileEntry> Profiles { get; set; } = [];
    }

    private sealed class OptimalProfileEntry
    {
        public string Id { get; set; } = "";
        public string Vendor { get; set; } = "";
        public string DisplayId { get; set; } = "";
        public string RecommendedPerformanceTier { get; set; } = "";
        public OptimalDisplayDefaults? Defaults { get; set; }
    }

    private sealed class OptimalDisplayDefaults
    {
        public double DepthScale { get; set; }
        public double ConvergenceOffset { get; set; }
        public string ComfortMode { get; set; } = "";
        public bool UiDepthCompensation { get; set; }
    }
}
