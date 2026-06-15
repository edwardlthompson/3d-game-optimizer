using System.Diagnostics;

namespace SpatialLabsOptimizer.Infrastructure.Launch;

public interface IProcessLauncher
{
    Task<bool> TryStartAsync(string fileName, string? arguments, CancellationToken cancellationToken = default);
    Task<bool> TryStartSteamGameAsync(int steamAppId, CancellationToken cancellationToken = default);
}

public sealed class ProcessLauncher : IProcessLauncher
{
    private readonly IGameInstallPathResolver _resolver;

    public ProcessLauncher(IGameInstallPathResolver resolver)
    {
        _resolver = resolver;
    }

    public Task<bool> TryStartSteamGameAsync(int steamAppId, CancellationToken cancellationToken = default)
    {
        var steamExe = _resolver.FindSteamExecutable();
        if (steamExe is null)
        {
            return Task.FromResult(false);
        }

        return TryStartAsync(steamExe, $"-applaunch {steamAppId}", cancellationToken);
    }

    public async Task<bool> TryStartAsync(string fileName, string? arguments, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName) || !File.Exists(fileName))
        {
            return false;
        }

        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments ?? string.Empty,
                UseShellExecute = false,
                CreateNoWindow = false,
                WorkingDirectory = Path.GetDirectoryName(fileName) ?? Environment.CurrentDirectory
            });

            if (process is null)
            {
                return false;
            }

            await Task.Delay(250, cancellationToken);
            return !process.HasExited || process.ExitCode == 0;
        }
        catch (Exception)
        {
            return false;
        }
    }
}

public sealed class ToolPathResolver
{
    private readonly string _toolsRoot;

    public ToolPathResolver(string? toolsRoot = null)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _toolsRoot = toolsRoot ?? Path.Combine(appData, "3d-game-optimizer", "tools");
    }

    public string? ResolveExecutable(string toolId, params string[] relativePaths)
    {
        foreach (var relative in relativePaths)
        {
            var candidate = Path.Combine(_toolsRoot, toolId, relative);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}
