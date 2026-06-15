using System.Diagnostics;
using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Progress;

namespace SpatialLabsOptimizer.Infrastructure.Install;

public sealed partial class SilentInstallOrchestrator : IProgressReportingOperation
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

        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = helperPath,
            Arguments = BuildHelperArgs(tool),
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
}
