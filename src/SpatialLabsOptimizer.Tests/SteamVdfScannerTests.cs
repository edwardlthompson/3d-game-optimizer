using SpatialLabsOptimizer.Infrastructure.Steam;

namespace SpatialLabsOptimizer.Tests;

public sealed class SteamVdfScannerTests
{
    [Fact]
    public void EnumerateSteamAppsPaths_IncludesDefaultAndLibraryFolders()
    {
        var root = Path.Combine(Path.GetTempPath(), $"3dgo-steam-{Guid.NewGuid()}");
        var secondary = Path.Combine(Path.GetTempPath(), $"3dgo-steam-lib-{Guid.NewGuid()}");
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "steamapps"));
            Directory.CreateDirectory(Path.Combine(secondary, "steamapps"));
            Directory.CreateDirectory(Path.Combine(root, "config"));
            var vdf = Path.Combine(root, "config", "libraryfolders.vdf");
            File.WriteAllText(
                vdf,
                "\"libraryfolders\"\n{\n\t\"0\"\n\t{\n\t\t\"path\"\t\t\"" +
                secondary.Replace("\\", "\\\\") +
                "\"\n\t}\n}");

            var paths = SteamVdfScanner.EnumerateSteamAppsPaths(root).ToList();
            Assert.Contains(Path.Combine(root, "steamapps"), paths);
            Assert.Contains(Path.Combine(secondary, "steamapps"), paths);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
            if (Directory.Exists(secondary)) Directory.Delete(secondary, recursive: true);
        }
    }

    [Fact]
    public void ScanInstalledAppIds_ReadsManifestsFromAllLibraries()
    {
        var root = Path.Combine(Path.GetTempPath(), $"3dgo-steam-scan-{Guid.NewGuid()}");
        var secondary = Path.Combine(Path.GetTempPath(), $"3dgo-steam-scan-lib-{Guid.NewGuid()}");
        try
        {
            var primaryApps = Path.Combine(root, "steamapps");
            var secondaryApps = Path.Combine(secondary, "steamapps");
            Directory.CreateDirectory(primaryApps);
            Directory.CreateDirectory(secondaryApps);
            Directory.CreateDirectory(Path.Combine(root, "config"));
            File.WriteAllText(
                Path.Combine(primaryApps, "appmanifest_620.acf"),
                "\"appid\"		\"620\"");
            File.WriteAllText(
                Path.Combine(secondaryApps, "appmanifest_730.acf"),
                "\"appid\"		\"730\"");
            File.WriteAllText(
                Path.Combine(root, "config", "libraryfolders.vdf"),
                "\"libraryfolders\"\n{\n\t\"1\"\n\t{\n\t\t\"path\"\t\t\"" +
                secondary.Replace("\\", "\\\\") +
                "\"\n\t}\n}");

            var scanner = new SteamVdfScanner();
            var appIds = scanner.ScanInstalledAppIds(root);
            Assert.Contains(620, appIds);
            Assert.Contains(730, appIds);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
            if (Directory.Exists(secondary)) Directory.Delete(secondary, recursive: true);
        }
    }
}
