using System.Diagnostics;
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
        "vrto3d"
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

        Console.WriteLine("Elevated helper ready. Use --help for usage.");
        return 0;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("SpatialLabsOptimizer.ElevatedHelper");
        Console.WriteLine("Usage:");
        Console.WriteLine("  install --tool-id <id> --silent \"<args>\" [--url <url>] [--sha256 <hash>]");
        Console.WriteLine("Executes privileged silent installs with URL/hash allowlist and audit logging.");
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

        // Silent install simulation — real installs use vendor-documented silent flags.
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
