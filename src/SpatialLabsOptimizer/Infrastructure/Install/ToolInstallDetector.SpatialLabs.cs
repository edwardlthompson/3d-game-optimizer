using SpatialLabsOptimizer.Infrastructure.Data;
using SpatialLabsOptimizer.Infrastructure.Launch;

namespace SpatialLabsOptimizer.Infrastructure.Install;

public sealed partial class ToolInstallDetector
{
    internal static bool IsExperienceCenterPresent()
    {
        var fileNames = new[]
        {
            "SpatialLabs Experience Center.exe",
            "Experience Center.exe",
            "SpatialLabsExperienceCenter.exe"
        };

        var roots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
        };

        foreach (var root in roots.Where(Directory.Exists))
        {
            try
            {
                foreach (var dir in Directory.EnumerateDirectories(root).Take(ProgramFilesSubdirScanLimit * 2))
                {
                    if (!dir.Contains("Acer", StringComparison.OrdinalIgnoreCase) &&
                        !dir.Contains("SpatialLabs", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    foreach (var name in fileNames)
                    {
                        if (File.Exists(Path.Combine(dir, name)) ||
                            Directory.EnumerateFiles(dir, name, SearchOption.AllDirectories).Any())
                        {
                            return true;
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (IOException)
            {
            }
        }

        return false;
    }

    private bool IsSpatialLabsRuntimeInstalled(ToolManifestDocument? manifest)
    {
        var tool = manifest?.Tools?.FirstOrDefault(t =>
            string.Equals(t.Id, "spatiallabs-runtime-platform", StringComparison.OrdinalIgnoreCase));
        if (tool?.Verification?.Type == "registryKey" &&
            IsRegistryKeyPresent(tool.Verification.PathHint))
        {
            return true;
        }

        return IsExperienceCenterPresent();
    }
}
