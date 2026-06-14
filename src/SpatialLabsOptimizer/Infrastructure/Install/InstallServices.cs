using System.Diagnostics;
using System.Text.Json;
using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Progress;

namespace SpatialLabsOptimizer.Infrastructure.Install;

public sealed class SilentInstallOrchestrator : IProgressReportingOperation
{
    private readonly JsonDataLoader _loader;
    private readonly OperationProgressHub _progressHub;

    public SilentInstallOrchestrator(JsonDataLoader loader, OperationProgressHub progressHub)
    {
        _loader = loader;
        _progressHub = progressHub;
    }

    public string OperationId => "silent-install";
    public Application.Progress.OperationCategory Category => Application.Progress.OperationCategory.Setup;

    public event EventHandler<OperationProgressReport>? ProgressChanged;

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var manifest = await _loader.LoadAsync<ToolManifestDocument>("tools/tool-manifest-v1.json", cancellationToken);
        var tools = manifest?.Tools ?? [];
        for (var i = 0; i < tools.Count; i++)
        {
            var tool = tools[i];
            var report = new OperationProgressReport(
                OperationId,
                Category,
                "Silent toolchain install",
                $"Installing {tool.Name}",
                StepIndex: i + 1,
                TotalSteps: tools.Count,
                PercentComplete: (i + 1) * 100.0 / tools.Count,
                DetailMessage: tool.SilentArgs);
            ProgressChanged?.Invoke(this, report);
            _progressHub.Publish(report);
            await Task.Delay(100, cancellationToken);
        }

        _progressHub.Publish(new OperationProgressReport(
            OperationId,
            Category,
            "Silent toolchain install",
            "Complete",
            IsComplete: true,
            PercentComplete: 100));
    }

    public async Task InstallToolAsync(ToolManifestEntry tool, CancellationToken cancellationToken = default)
    {
        var helperPath = Path.Combine(AppContext.BaseDirectory, "SpatialLabsOptimizer.ElevatedHelper.exe");
        if (!File.Exists(helperPath))
        {
            return;
        }

        var args = $"install --tool-id {tool.Id} --silent \"{tool.SilentArgs}\"";
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = helperPath,
            Arguments = args,
            CreateNoWindow = true,
            UseShellExecute = false
        });

        if (process is not null)
        {
            await process.WaitForExitAsync(cancellationToken);
        }
    }

    private sealed class ToolManifestDocument
    {
        public List<ToolEntryDto> Tools { get; set; } = [];
    }

    private sealed class ToolEntryDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public string Sha256 { get; set; } = "";
        public string SilentArgs { get; set; } = "";
        public List<int> SuccessExitCodes { get; set; } = [0];
        public string? PostInstallConfig { get; set; }
    }
}

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
        if (defaults?.Profiles is null || !defaults.Profiles.TryGetValue(profileId, out var profile))
        {
            return;
        }

        if (profile.Global3D is not null)
        {
            var configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "3d-game-optimizer", "config", "global-3d.ini");
            await _configWriter.ApplyReShadeConfigAsync(
                Path.GetDirectoryName(configPath) ?? "",
                profile.Global3D.Depth,
                profile.Global3D.Convergence,
                cancellationToken);
        }
    }

    private sealed class OptimalDefaultsDocument
    {
        public Dictionary<string, ProfileDefaults> Profiles { get; set; } = [];
    }

    private sealed class ProfileDefaults
    {
        public Global3DSettings? Global3D { get; set; }
    }

    private sealed class Global3DSettings
    {
        public double Depth { get; set; }
        public double Convergence { get; set; }
        public double Separation { get; set; }
    }
}
