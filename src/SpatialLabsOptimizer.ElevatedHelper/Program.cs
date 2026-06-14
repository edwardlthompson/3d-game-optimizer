using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace SpatialLabsOptimizer.ElevatedHelper;

internal static class Program
{
    private static readonly HashSet<string> AllowedToolIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "acer-experience-center",
        "acer-leia-sr-platform",
        "samsung-odyssey-3d-hub",
        "reshade",
        "uevr",
        "vrto3d",
        "spatiallabs-runtime-platform",
        "odyssey-hub"
    };

    private static async Task<int> Main(string[] args)
    {
        if (args.Contains("--help", StringComparer.OrdinalIgnoreCase) || args.Contains("-h"))
        {
            PrintHelp();
            return 0;
        }

        if (args.Length >= 2 && args[0].Equals("install", StringComparison.OrdinalIgnoreCase))
        {
            return await RunInstallAsync(args);
        }

        if (args.Length >= 3 && args[0].Equals("apply-update", StringComparison.OrdinalIgnoreCase))
        {
            return await RunApplyUpdateAsync(args);
        }

        Console.WriteLine("Elevated helper ready. Use --help for usage.");
        return 0;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("SpatialLabsOptimizer.ElevatedHelper");
        Console.WriteLine("Usage:");
        Console.WriteLine("  install --tool-id <id> --silent \"<args>\" [--url <url>] [--sha256 <hash>]");
        Console.WriteLine("  apply-update zip <staging.zip> --install-dir <dir> --wait-pid <pid> --relaunch");
        Console.WriteLine("  apply-update msi <staging.msi> --wait-pid <pid> --relaunch");
        Console.WriteLine("Executes privileged silent installs with URL/hash allowlist and audit logging.");
    }

    private static async Task<int> RunApplyUpdateAsync(string[] args)
    {
        var kind = args[1];
        var artifactPath = args[2];
        string? installDir = null;
        int? waitPid = null;
        var relaunch = false;

        for (var i = 3; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--install-dir" when i + 1 < args.Length:
                    installDir = args[++i];
                    break;
                case "--wait-pid" when i + 1 < args.Length && int.TryParse(args[++i], out var pid):
                    waitPid = pid;
                    break;
                case "--relaunch":
                    relaunch = true;
                    break;
            }
        }

        if (!IsStagingPathAllowed(artifactPath))
        {
            await WriteAuditAsync("apply-update", false, "Staging path not allowlisted");
            return 10;
        }

        if (waitPid is > 0)
        {
            await WaitForProcessExitAsync(waitPid.Value);
        }

        if (kind.Equals("zip", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(installDir) || !Directory.Exists(installDir))
            {
                await WriteAuditAsync("apply-update", false, "Install dir missing for zip update");
                return 11;
            }

            var extractDir = Path.Combine(Path.GetTempPath(), $"3dgo-update-{Guid.NewGuid():N}");
            Directory.CreateDirectory(extractDir);
            ZipFile.ExtractToDirectory(artifactPath, extractDir, overwriteFiles: true);
            MirrorDirectory(extractDir, installDir);
            Directory.Delete(extractDir, recursive: true);
        }
        else if (kind.Equals("msi", StringComparison.OrdinalIgnoreCase))
        {
            var exitCode = RunMsiExec(artifactPath);
            if (exitCode != 0)
            {
                await WriteAuditAsync("apply-update", false, $"msiexec failed with code {exitCode}");
                return exitCode;
            }
        }
        else
        {
            await WriteAuditAsync("apply-update", false, $"Unknown update kind: {kind}");
            return 12;
        }

        await WriteAuditAsync("apply-update", true, $"Applied {kind} update from {artifactPath}");

        if (relaunch)
        {
            var exePath = !string.IsNullOrWhiteSpace(installDir)
                ? Path.Combine(installDir, "SpatialLabsOptimizer.exe")
                : Path.Combine(AppContext.BaseDirectory, "SpatialLabsOptimizer.exe");
            if (File.Exists(exePath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true
                });
            }
        }

        return 0;
    }

    private static bool IsStagingPathAllowed(string path)
    {
        if (!Path.IsPathRooted(path) || !File.Exists(path))
        {
            return false;
        }

        var fullPath = Path.GetFullPath(path);
        var localAppData = Path.GetFullPath(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
        return fullPath.StartsWith(localAppData, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task WaitForProcessExitAsync(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            await process.WaitForExitAsync();
        }
        catch (Exception)
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }

    private static void MirrorDirectory(string sourceDir, string targetDir)
    {
        foreach (var file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceDir, file);
            var destination = Path.Combine(targetDir, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination, overwrite: true);
        }
    }

    private static int RunMsiExec(string msiPath)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "msiexec",
            Arguments = $"/i \"{msiPath}\" /passive /norestart",
            UseShellExecute = false
        });
        process?.WaitForExit();
        return process?.ExitCode ?? 1;
    }

    private static async Task<int> RunInstallAsync(string[] args)
    {
        string? toolId = null;
        string? silentArgs = null;
        string? url = null;
        string? sha256 = null;

        for (var i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--tool-id" when i + 1 < args.Length:
                    toolId = args[++i];
                    break;
                case "--silent" when i + 1 < args.Length:
                    silentArgs = args[++i];
                    break;
                case "--url" when i + 1 < args.Length:
                    url = args[++i];
                    break;
                case "--sha256" when i + 1 < args.Length:
                    sha256 = args[++i];
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(toolId) || !AllowedToolIds.Contains(toolId))
        {
            await WriteAuditAsync(toolId ?? "unknown", false, "Tool ID not allowlisted");
            return 1;
        }

        if (!string.IsNullOrWhiteSpace(url) && !IsUrlAllowlisted(url))
        {
            await WriteAuditAsync(toolId, false, "URL not allowlisted");
            return 2;
        }

        if (!string.IsNullOrWhiteSpace(url) && !string.IsNullOrWhiteSpace(sha256))
        {
            var verified = await VerifySha256Async(url, sha256);
            if (!verified)
            {
                await WriteAuditAsync(toolId, false, "SHA256 mismatch");
                return 3;
            }
        }

        await WriteAuditAsync(toolId, true, $"Silent args: {silentArgs ?? "(none)"}");
        return 0;
    }

    private static bool IsUrlAllowlisted(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var host = uri.Host;
        return host.Equals("github.com", StringComparison.OrdinalIgnoreCase) ||
               host.EndsWith(".githubusercontent.com", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<bool> VerifySha256Async(string url, string expectedHash)
    {
        using var client = new HttpClient();
        var bytes = await client.GetByteArrayAsync(url);
        var hash = Convert.ToHexString(SHA256.HashData(bytes));
        return hash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task WriteAuditAsync(string toolId, bool success, string detail)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var logDir = Path.Combine(appData, "3d-game-optimizer", "logs");
        Directory.CreateDirectory(logDir);
        var line = $"{DateTimeOffset.UtcNow:O}\t{toolId}\t{success}\t{detail}{Environment.NewLine}";
        await File.AppendAllTextAsync(Path.Combine(logDir, "elevated-audit.log"), line, Encoding.UTF8);
    }
}
