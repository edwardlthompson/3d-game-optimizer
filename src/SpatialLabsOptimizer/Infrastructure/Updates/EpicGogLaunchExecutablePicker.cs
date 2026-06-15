namespace SpatialLabsOptimizer.Infrastructure.Updates;

internal static class EpicGogLaunchExecutablePicker
{
    internal static string? PickLaunchExecutable(string installDir)
    {
        if (!Directory.Exists(installDir))
        {
            return null;
        }

        var candidates = Directory.EnumerateFiles(installDir, "*.exe", SearchOption.TopDirectoryOnly)
            .Where(p => !IsExcludedExecutable(Path.GetFileName(p)))
            .Select(p => new FileInfo(p))
            .OrderByDescending(f => f.Length)
            .ToList();

        return candidates.FirstOrDefault()?.FullName;
    }

    private static bool IsExcludedExecutable(string fileName)
        => fileName.StartsWith("unins", StringComparison.OrdinalIgnoreCase)
           || fileName.StartsWith("setup", StringComparison.OrdinalIgnoreCase)
           || fileName.Contains("redist", StringComparison.OrdinalIgnoreCase);
}
