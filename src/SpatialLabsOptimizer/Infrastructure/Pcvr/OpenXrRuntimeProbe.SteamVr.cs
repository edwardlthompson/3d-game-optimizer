using System.Text.Json;

namespace SpatialLabsOptimizer.Infrastructure.Pcvr;

public static partial class OpenXrRuntimeProbe
{
    public static string? TryResolveSteamVrRootFromRuntimeJson(string runtimeJsonPath)
    {
        if (!File.Exists(runtimeJsonPath))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(runtimeJsonPath));
            if (!doc.RootElement.TryGetProperty("runtime", out var runtime))
            {
                return null;
            }

            string? libraryPath = null;
            if (runtime.TryGetProperty("library_path", out var libraryPathProp))
            {
                libraryPath = libraryPathProp.GetString();
            }

            if (string.IsNullOrWhiteSpace(libraryPath))
            {
                var runtimeName = TryParseRuntimeName(runtimeJsonPath);
                return runtimeName?.Contains("SteamVR", StringComparison.OrdinalIgnoreCase) == true
                    ? Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                        "Steam", "steamapps", "common", "SteamVR")
                    : null;
            }

            libraryPath = Environment.ExpandEnvironmentVariables(libraryPath.Trim('"'));
            var dir = Path.GetDirectoryName(libraryPath);
            while (!string.IsNullOrWhiteSpace(dir))
            {
                if (File.Exists(Path.Combine(dir, "vrstartup.exe")) ||
                    File.Exists(Path.Combine(dir, "bin", "win64", "vrstartup.exe")))
                {
                    return dir;
                }

                if (string.Equals(Path.GetFileName(dir), "SteamVR", StringComparison.OrdinalIgnoreCase))
                {
                    return dir;
                }

                dir = Path.GetDirectoryName(dir);
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
}
