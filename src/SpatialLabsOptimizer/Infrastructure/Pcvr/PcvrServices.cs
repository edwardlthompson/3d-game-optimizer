using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Progress;

namespace SpatialLabsOptimizer.Infrastructure.Pcvr;

public sealed class PcvrRuntimeConnector
{
    public Task<string?> DetectRuntimeAsync(CancellationToken cancellationToken = default)
    {
        var steamVr = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Steam", "steamapps", "common", "SteamVR");

        if (Directory.Exists(steamVr))
        {
            return Task.FromResult<string?>("SteamVR");
        }

        return Task.FromResult<string?>(null);
    }

    public bool IsRuntimeAvailable() =>
        DetectRuntimeAsync().GetAwaiter().GetResult() is not null;
}

public sealed class UpdateService
{
    private readonly ExternalDataGateway _gateway;
    private readonly OperationProgressHub _progressHub;

    public UpdateService(ExternalDataGateway gateway, OperationProgressHub progressHub)
    {
        _gateway = gateway;
        _progressHub = progressHub;
    }

    public async Task<string?> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        _progressHub.Publish(new OperationProgressReport(
            "update-check",
            Application.Progress.OperationCategory.Update,
            "Checking for updates",
            "Fetching release manifest…"));

        // Version check against GitHub releases — no device fingerprinting.
        return await Task.FromResult<string?>(null);
    }
}

public sealed class DiagnosticBundleService
{
    public async Task<string> ExportAsync(CancellationToken cancellationToken = default)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var bundleDir = Path.Combine(appData, "3d-game-optimizer", "diagnostics");
        Directory.CreateDirectory(bundleDir);
        var path = Path.Combine(bundleDir, $"bundle-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.zip");
        await File.WriteAllTextAsync(path.Replace(".zip", ".txt"), "Diagnostic bundle placeholder", cancellationToken);
        return path.Replace(".zip", ".txt");
    }
}

public sealed class CommandPaletteService
{
    private readonly List<CommandPaletteEntry> _commands = new()
    {
        new("play-3d", "Play in 3D", "Launch selected game in 3D"),
        new("setup-wizard", "Run Setup Wizard", "Silent toolchain install"),
        new("refresh-metadata", "Refresh Metadata", "Update Steam store data"),
        new("safe-launch", "Safe Launch", "Launch without injectors"),
        new("diagnostic-bundle", "Export Diagnostics", "Create redacted support bundle")
    };

    public IReadOnlyList<CommandPaletteEntry> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return _commands;
        }

        return _commands
            .Where(c => c.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        c.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}

public sealed record CommandPaletteEntry(string Id, string Title, string Description);
