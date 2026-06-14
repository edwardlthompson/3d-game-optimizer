using System.Diagnostics;
using System.Text.RegularExpressions;
using SpatialLabsOptimizer.Domain;
using SpatialLabsOptimizer.Infrastructure.Install;
using SpatialLabsOptimizer.Infrastructure.Launch.Coexistence;

namespace SpatialLabsOptimizer.Infrastructure.Launch;

public sealed record GameInstallInfo(string InstallDir, string? LaunchExecutable);

public interface IGameInstallPathResolver
{
    GameInstallInfo? Resolve(int steamAppId);
    string? FindSteamExecutable();
}

public sealed class GameInstallPathResolver : IGameInstallPathResolver
{
    private static readonly Regex AppIdRegex = new("\"appid\"\\s+\"(\\d+)\"", RegexOptions.Compiled);
    private static readonly Regex InstallDirRegex = new("\"installdir\"\\s+\"([^\"]+)\"", RegexOptions.Compiled);
    private static readonly Regex LaunchExeRegex = new("\"launch\"\\s+\"([^\"]+)\"", RegexOptions.Compiled);

    public GameInstallInfo? Resolve(int steamAppId)
    {
        var steamPath = FindSteamExecutable();
        if (steamPath is null)
        {
            return null;
        }

        var steamRoot = Path.GetDirectoryName(steamPath);
        if (steamRoot is null)
        {
            return null;
        }

        var manifestPath = Path.Combine(steamRoot, "steamapps", $"appmanifest_{steamAppId}.acf");
        if (!File.Exists(manifestPath))
        {
            return null;
        }

        var content = File.ReadAllText(manifestPath);
        var appMatch = AppIdRegex.Match(content);
        if (!appMatch.Success || !int.TryParse(appMatch.Groups[1].Value, out var parsedId) || parsedId != steamAppId)
        {
            return null;
        }

        var dirMatch = InstallDirRegex.Match(content);
        if (!dirMatch.Success)
        {
            return null;
        }

        var installDir = Path.Combine(steamRoot, "steamapps", "common", dirMatch.Groups[1].Value);
        if (!Directory.Exists(installDir))
        {
            return null;
        }

        string? launchExe = null;
        var launchMatch = LaunchExeRegex.Match(content);
        if (launchMatch.Success)
        {
            var candidate = Path.Combine(installDir, launchMatch.Groups[1].Value);
            if (File.Exists(candidate))
            {
                launchExe = candidate;
            }
        }

        launchExe ??= Directory.EnumerateFiles(installDir, "*.exe", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(p => !Path.GetFileName(p).StartsWith("unins", StringComparison.OrdinalIgnoreCase));

        return new GameInstallInfo(installDir, launchExe);
    }

    public string? FindSteamExecutable()
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var candidate = Path.Combine(programFiles, "Steam", "steam.exe");
        return File.Exists(candidate) ? candidate : null;
    }
}

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

public abstract class LaunchAdapterBase
{
    public abstract LaunchPlatform Platform { get; }

    public abstract Task<bool> LaunchAsync(
        ResolvedGameLaunchPlan plan,
        LaunchContext context,
        CancellationToken cancellationToken = default);
}

public sealed class TrueGameLauncher : LaunchAdapterBase
{
    private readonly IGameInstallPathResolver _installPaths;
    private readonly IProcessLauncher _launcher;

    public TrueGameLauncher(IGameInstallPathResolver installPaths, IProcessLauncher launcher)
    {
        _installPaths = installPaths;
        _launcher = launcher;
    }

    public override LaunchPlatform Platform => LaunchPlatform.TrueGame;

    public override async Task<bool> LaunchAsync(
        ResolvedGameLaunchPlan plan,
        LaunchContext context,
        CancellationToken cancellationToken = default)
    {
        var install = _installPaths.Resolve(plan.SteamAppId);
        if (install?.LaunchExecutable is not null)
        {
            return await _launcher.TryStartAsync(install.LaunchExecutable, null, cancellationToken);
        }

        return await _launcher.TryStartSteamGameAsync(plan.SteamAppId, cancellationToken);
    }
}

public sealed class UevrLauncher : LaunchAdapterBase
{
    private readonly IGameInstallPathResolver _installPaths;
    private readonly IProcessLauncher _launcher;
    private readonly ToolPathResolver _toolPaths;

    public UevrLauncher(
        IGameInstallPathResolver installPaths,
        IProcessLauncher launcher,
        ToolPathResolver toolPaths)
    {
        _installPaths = installPaths;
        _launcher = launcher;
        _toolPaths = toolPaths;
    }

    public override LaunchPlatform Platform => LaunchPlatform.Uevr;

    public override async Task<bool> LaunchAsync(
        ResolvedGameLaunchPlan plan,
        LaunchContext context,
        CancellationToken cancellationToken = default)
    {
        var install = _installPaths.Resolve(plan.SteamAppId);
        if (context.IsGameFirst)
        {
            if (install?.LaunchExecutable is not null)
            {
                return await _launcher.TryStartAsync(install.LaunchExecutable, null, cancellationToken);
            }

            return await _launcher.TryStartSteamGameAsync(plan.SteamAppId, cancellationToken);
        }

        var injector = _toolPaths.ResolveExecutable("uevr", "UEVRInjector.exe", "bin/UEVRInjector.exe");
        if (injector is not null && install?.LaunchExecutable is not null)
        {
            var args = $"\"{install.LaunchExecutable}\"";
            if (await _launcher.TryStartAsync(injector, args, cancellationToken))
            {
                return true;
            }
        }

        return await _launcher.TryStartSteamGameAsync(plan.SteamAppId, cancellationToken);
    }
}

public sealed class ReShadeLauncher : LaunchAdapterBase
{
    private readonly IGameInstallPathResolver _installPaths;
    private readonly IProcessLauncher _launcher;
    private readonly ToolConfigWriter _configWriter;

    public ReShadeLauncher(
        IGameInstallPathResolver installPaths,
        IProcessLauncher launcher,
        ToolConfigWriter configWriter)
    {
        _installPaths = installPaths;
        _launcher = launcher;
        _configWriter = configWriter;
    }

    public override LaunchPlatform Platform => LaunchPlatform.ReShade;

    public override async Task<bool> LaunchAsync(
        ResolvedGameLaunchPlan plan,
        LaunchContext context,
        CancellationToken cancellationToken = default)
    {
        var install = _installPaths.Resolve(plan.SteamAppId);
        if (install is not null && !context.IsGameFirst)
        {
            await _configWriter.ApplyReShadeConfigAsync(install.InstallDir, plan.Depth, plan.Convergence, cancellationToken);
        }

        if (install?.LaunchExecutable is not null)
        {
            return await _launcher.TryStartAsync(install.LaunchExecutable, null, cancellationToken);
        }

        return await _launcher.TryStartSteamGameAsync(plan.SteamAppId, cancellationToken);
    }
}

public sealed class LaunchAdapterRegistry
{
    private readonly IReadOnlyList<LaunchAdapterBase> _adapters;

    public LaunchAdapterRegistry(IEnumerable<LaunchAdapterBase> adapters)
    {
        _adapters = adapters.ToList();
    }

    public LaunchAdapterBase? GetAdapter(LaunchPlatform platform) =>
        _adapters.FirstOrDefault(a => a.Platform == platform);
}
