using System.Text.Json;
using Microsoft.Win32;

namespace SpatialLabsOptimizer.Infrastructure.Pcvr;

public static partial class OpenXrRuntimeProbe
{
    private const string KhronosOpenXrKey = @"SOFTWARE\Khronos\OpenXR\1";
    private const string MicrosoftOpenXrKey = @"Software\Microsoft\Windows\CurrentVersion\OpenXR";

    public static Func<string?>? TestRuntimePathResolver { get; set; }

    private static readonly Dictionary<string, string> OverrideLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["steamvr"] = "OpenXR:SteamVR/OpenXR",
        ["meta-link"] = "OpenXR:Meta Quest Link",
        ["virtual-desktop"] = "OpenXR:Virtual Desktop"
    };

    public static string? TryResolveActiveRuntimeLabel(string? overrideId = null, bool skipOverride = false)
    {
        if (!skipOverride && string.Equals(overrideId, "off", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!skipOverride && !string.IsNullOrWhiteSpace(overrideId) &&
            !string.Equals(overrideId, "auto", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(overrideId, "detected", StringComparison.OrdinalIgnoreCase))
            {
                return TryResolveActiveRuntimeLabel(skipOverride: true);
            }

            if (OverrideLabels.TryGetValue(overrideId, out var label))
            {
                return label;
            }
        }

        var runtimePath = TryGetActiveRuntimeJsonPath();
        if (string.IsNullOrWhiteSpace(runtimePath) || !File.Exists(runtimePath))
        {
            return null;
        }

        var runtimeName = TryParseRuntimeName(runtimePath);
        return runtimeName is null ? "OpenXR" : $"OpenXR:{runtimeName}";
    }

    public static string? TryGetActiveRuntimeJsonPath()
    {
        if (TestRuntimePathResolver is not null)
        {
            var overridePath = TestRuntimePathResolver();
            if (!string.IsNullOrWhiteSpace(overridePath))
            {
                return overridePath;
            }
        }

        foreach (var (hive, subKey) in new[]
        {
            (RegistryHive.LocalMachine, KhronosOpenXrKey),
            (RegistryHive.CurrentUser, MicrosoftOpenXrKey),
            (RegistryHive.LocalMachine, MicrosoftOpenXrKey)
        })
        {
            var path = ReadRegistryRuntimePath(hive, subKey);
            if (!string.IsNullOrWhiteSpace(path))
            {
                return path;
            }
        }

        var defaultPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "OpenXR", "runtime.json");
        return File.Exists(defaultPath) ? defaultPath : null;
    }

    public static string? TryParseRuntimeName(string runtimeJsonPath)
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(runtimeJsonPath));
            var root = doc.RootElement;
            if (root.TryGetProperty("runtime", out var runtime) &&
                runtime.TryGetProperty("name", out var nameProp))
            {
                return nameProp.GetString();
            }

            if (root.TryGetProperty("name", out var flatName))
            {
                return flatName.GetString();
            }
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }

        return null;
    }

    private static string? ReadRegistryRuntimePath(RegistryHive hive, string subKey)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            using var key = baseKey.OpenSubKey(subKey);
            foreach (var valueName in new[] { "ActiveRuntime", "ActiveRuntimePath", "ActiveRuntimeJson" })
            {
                var value = key?.GetValue(valueName) as string;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return Environment.ExpandEnvironmentVariables(value.Trim('"'));
                }
            }
        }
        catch (Exception)
        {
            return null;
        }

        return null;
    }
}
