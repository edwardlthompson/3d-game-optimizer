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
        Console.WriteLine("  install --tool-id <id> --silent \"<args>\" [--url <url>] [--local-package <path>] [--sha256 <hash>]");
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
        string? localPackage = null;
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
                case "--local-package" when i + 1 < args.Length:
                    localPackage = args[++i];
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

        if (string.IsNullOrWhiteSpace(url) && string.IsNullOrWhiteSpace(localPackage))
        {
            await WriteAuditAsync(toolId, false, "Download URL or local package required");
            return 4;
        }

        if (string.IsNullOrWhiteSpace(sha256))
        {
            await WriteAuditAsync(toolId, false, "SHA256 required");
            return 6;
        }

        byte[] bytes;
        if (!string.IsNullOrWhiteSpace(localPackage))
        {
            if (!IsBundledPackagePathAllowed(localPackage))
            {
                await WriteAuditAsync(toolId, false, "Local package path not allowlisted");
                return 7;
            }

            bytes = await File.ReadAllBytesAsync(localPackage);
        }
        else
        {
            if (!IsUrlAllowlisted(url!))
            {
                await WriteAuditAsync(toolId, false, "URL not allowlisted");
                return 2;
            }

            try
            {
                bytes = await DownloadAllowlistedAsync(url!);
            }
            catch (Exception ex)
            {
                await WriteAuditAsync(toolId, false, $"Download failed: {ex.Message}");
                return 5;
            }
        }

        var hash = Convert.ToHexString(SHA256.HashData(bytes));
        if (!hash.Equals(sha256, StringComparison.OrdinalIgnoreCase))
        {
            await WriteAuditAsync(toolId, false, "SHA256 mismatch");
            return 3;
        }

        var toolsRoot = GetToolInstallRoot(toolId);
        Directory.CreateDirectory(toolsRoot);
        var stagingDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "3d-game-optimizer",
            "staging",
            toolId);
        Directory.CreateDirectory(stagingDir);

        var extension = GuessExtension(url ?? localPackage ?? "", bytes);
        var stagingFile = Path.Combine(stagingDir, $"package{extension}");
        await File.WriteAllBytesAsync(stagingFile, bytes);

        var exitCode = extension switch
        {
            ".zip" => ExtractZip(stagingFile, toolsRoot),
            ".msi" => RunMsiExec(stagingFile),
            ".exe" => RunSilentExecutable(stagingFile, silentArgs),
            _ => CopyPayload(stagingFile, toolsRoot)
        };

        await WriteAuditAsync(
            toolId,
            exitCode == 0,
            exitCode == 0
                ? $"Installed to {toolsRoot} via {extension}"
                : $"Install failed with exit code {exitCode}");
        return exitCode;
    }

    private static string GetToolInstallRoot(string toolId) =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "3d-game-optimizer",
            "tools",
            toolId);

    private static async Task<byte[]> DownloadAllowlistedAsync(string url)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
        return await client.GetByteArrayAsync(url);
    }

    private static string GuessExtension(string url, byte[] bytes)
    {
        if (bytes.Length >= 2 && bytes[0] == 0x50 && bytes[1] == 0x4B)
        {
            return ".zip";
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var ext = Path.GetExtension(uri.AbsolutePath);
            if (!string.IsNullOrWhiteSpace(ext))
            {
                return ext.ToLowerInvariant();
            }
        }

        return ".bin";
    }

    private static int ExtractZip(string zipPath, string targetDir)
    {
        try
        {
            ZipFile.ExtractToDirectory(zipPath, targetDir, overwriteFiles: true);
            return 0;
        }
        catch (IOException)
        {
            return -2;
        }
        catch (InvalidDataException)
        {
            return -2;
        }
    }

    private static int CopyPayload(string sourceFile, string targetDir)
    {
        var destination = Path.Combine(targetDir, Path.GetFileName(sourceFile));
        File.Copy(sourceFile, destination, overwrite: true);
        return 0;
    }

    private static int RunSilentExecutable(string exePath, string? silentArgs)
    {
        var arguments = string.IsNullOrWhiteSpace(silentArgs) ? "/S" : silentArgs;
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = arguments,
            CreateNoWindow = true,
            UseShellExecute = false
        });
        process?.WaitForExit();
        return process?.ExitCode ?? 1;
    }

    private static bool IsBundledPackagePathAllowed(string path)
    {
        if (!Path.IsPathRooted(path) || !File.Exists(path))
        {
            return false;
        }

        var fullPath = Path.GetFullPath(path);
        var markers = new[]
        {
            $"{Path.DirectorySeparatorChar}tools{Path.DirectorySeparatorChar}fixtures{Path.DirectorySeparatorChar}",
            $"{Path.AltDirectorySeparatorChar}tools{Path.AltDirectorySeparatorChar}fixtures{Path.AltDirectorySeparatorChar}"
        };
        return markers.Any(marker => fullPath.Contains(marker, StringComparison.OrdinalIgnoreCase));
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

    private static async Task WriteAuditAsync(string toolId, bool success, string detail)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var logDir = Path.Combine(appData, "3d-game-optimizer", "logs");
        Directory.CreateDirectory(logDir);
        var line = $"{DateTimeOffset.UtcNow:O}\t{toolId}\t{success}\t{detail}{Environment.NewLine}";
        await File.AppendAllTextAsync(Path.Combine(logDir, "elevated-audit.log"), line, Encoding.UTF8);
    }
}
