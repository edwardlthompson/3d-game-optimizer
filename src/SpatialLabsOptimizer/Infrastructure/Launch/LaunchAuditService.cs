using System.Text;
using SpatialLabsOptimizer.Domain;

namespace SpatialLabsOptimizer.Infrastructure.Launch;

public sealed class LaunchAuditService
{
    private readonly string _logPath;

    public LaunchAuditService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var logDir = Path.Combine(appData, "3d-game-optimizer", "logs");
        Directory.CreateDirectory(logDir);
        _logPath = Path.Combine(logDir, "launch-audit.log");
    }

    public async Task LogAsync(
        int appId,
        string title,
        LaunchPlatform platform,
        bool success,
        string? errorCode = null,
        string? fallbackNote = null,
        CancellationToken cancellationToken = default)
    {
        var line = new StringBuilder()
            .Append(DateTimeOffset.UtcNow.ToString("O"))
            .Append('\t').Append(appId)
            .Append('\t').Append(title)
            .Append('\t').Append(platform)
            .Append('\t').Append(success ? "OK" : "FAIL")
            .Append('\t').Append(errorCode ?? "")
            .Append('\t').Append(fallbackNote ?? "")
            .AppendLine();
        await File.AppendAllTextAsync(_logPath, line.ToString(), cancellationToken);
    }

    public string LogPath => _logPath;
}

public sealed class AutoFallbackLaunchService
{
    private readonly LaunchAdapterRegistry _adapters;

    public AutoFallbackLaunchService(LaunchAdapterRegistry adapters)
    {
        _adapters = adapters;
    }

    public async Task<(bool Success, LaunchPlatform UsedPlatform, string? Note)> LaunchWithFallbackAsync(
        ResolvedGameLaunchPlan plan,
        CancellationToken cancellationToken = default)
    {
        var primary = _adapters.GetAdapter(plan.Platform);
        if (primary is not null && await primary.LaunchAsync(plan, cancellationToken))
        {
            return (true, plan.Platform, null);
        }

        if (plan.Platform == LaunchPlatform.ReShade)
        {
            var uevr = _adapters.GetAdapter(LaunchPlatform.Uevr);
            if (uevr is not null && await uevr.LaunchAsync(plan with { Platform = LaunchPlatform.Uevr }, cancellationToken))
            {
                return (true, LaunchPlatform.Uevr, "Auto-fallback: ReShade → UEVR");
            }
        }

        return (false, plan.Platform, "Primary and fallback launch failed");
    }
}
