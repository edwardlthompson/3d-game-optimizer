using System.Diagnostics;
using System.Text.Json;
using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Progress;

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

public sealed class SilentInstallOrchestrator : IProgressReportingOperation
{
    private readonly JsonDataLoader _loader;
    private readonly OperationProgressHub _progressHub;
    private readonly InstallErrorCatalog _errors;
    private readonly IElevatedHelperLocator _helperLocator;

    public SilentInstallOrchestrator(
        JsonDataLoader loader,
        OperationProgressHub progressHub,
        InstallErrorCatalog errors,
        IElevatedHelperLocator helperLocator)
    {
        _loader = loader;
        _progressHub = progressHub;
        _errors = errors;
        _helperLocator = helperLocator;
    }

    public string OperationId => "silent-install";
    public Application.Progress.OperationCategory Category => Application.Progress.OperationCategory.Setup;

    public event EventHandler<OperationProgressReport>? ProgressChanged;

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        => await ExecuteAsync(null, cancellationToken);

    public async Task ExecuteAsync(IReadOnlyList<string>? toolFilter, CancellationToken cancellationToken = default)
    {
        var manifest = await _loader.LoadAsync<ToolManifestDocument>("tools/tool-manifest-v1.json", cancellationToken);
        var tools = manifest?.Tools?.Select(t => t.ToEntry()).ToList() ?? [];
        if (toolFilter is { Count: > 0 })
        {
            var filter = toolFilter.ToHashSet(StringComparer.OrdinalIgnoreCase);
            tools = tools.Where(t => filter.Contains(t.Id)).ToList();
        }
        var helperPath = _helperLocator.HelperPath;
        var helperMissing = !File.Exists(helperPath);

        for (var i = 0; i < tools.Count; i++)
        {
            var tool = tools[i];
            if (string.IsNullOrWhiteSpace(tool.DownloadUrl) && string.IsNullOrWhiteSpace(tool.BundledPackage))
            {
                var skip = new OperationProgressReport(
                    OperationId,
                    Category,
                    "Silent toolchain install",
                    $"Skipped {tool.Name} — install manually from vendor site",
                    StepIndex: i + 1,
                    TotalSteps: tools.Count,
                    PercentComplete: (i + 1) * 100.0 / tools.Count,
                    DetailMessage: tool.SilentArgs);
                ProgressChanged?.Invoke(this, skip);
                _progressHub.Publish(skip);
                continue;
            }

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

            var exitCode = await InstallToolAsync(tool, cancellationToken);
            var classified = _errors.Classify(exitCode, helperMissing);
            if (exitCode != 0)
            {
                var failed = new OperationProgressReport(
                    OperationId,
                    Category,
                    "Silent toolchain install",
                    classified.Message,
                    StepIndex: i + 1,
                    TotalSteps: tools.Count,
                    IsFailed: true,
                    ErrorMessage: $"{classified.Code}: {classified.Recovery}");
                ProgressChanged?.Invoke(this, failed);
                _progressHub.Publish(failed);
                return;
            }
        }

        _progressHub.Publish(new OperationProgressReport(
            OperationId,
            Category,
            "Silent toolchain install",
            "Complete",
            IsComplete: true,
            PercentComplete: 100));
    }

    public async Task<int> InstallToolAsync(ToolManifestEntry tool, CancellationToken cancellationToken = default)
    {
        var helperPath = _helperLocator.HelperPath;
        if (!File.Exists(helperPath))
        {
            return -1;
        }

        var args = BuildHelperArgs(tool);
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = helperPath,
            Arguments = args,
            CreateNoWindow = true,
            UseShellExecute = false
        });

        if (process is null)
        {
            return -2;
        }

        await process.WaitForExitAsync(cancellationToken);
        return tool.SuccessExitCodes.Contains(process.ExitCode) ? 0 : process.ExitCode;
    }

    private string BuildHelperArgs(ToolManifestEntry tool)
    {
        var builder = new System.Text.StringBuilder();
        builder.Append("install --tool-id ").Append(tool.Id).Append(" --silent \"").Append(tool.SilentArgs).Append('"');
        if (!string.IsNullOrWhiteSpace(tool.DownloadUrl))
        {
            builder.Append(" --url \"").Append(tool.DownloadUrl).Append('"');
        }

        if (!string.IsNullOrWhiteSpace(tool.BundledPackage))
        {
            var bundledPath = Path.GetFullPath(Path.Combine(_loader.DataRoot, tool.BundledPackage));
            builder.Append(" --local-package \"").Append(bundledPath).Append('"');
        }

        if (!string.IsNullOrWhiteSpace(tool.Sha256))
        {
            builder.Append(" --sha256 ").Append(tool.Sha256);
        }

        return builder.ToString();
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
