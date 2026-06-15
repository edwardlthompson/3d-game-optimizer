using SpatialLabsOptimizer.Infrastructure.Pcvr;

namespace SpatialLabsOptimizer.Tests;

public class PcvrOpenXrTests
{
    [Fact]
    public void OpenXrProbe_ParseRuntimeJson_ExtractsRuntimeName()
    {
        var path = Path.Combine(Path.GetTempPath(), $"3dgo-openxr-{Guid.NewGuid()}.json");
        File.WriteAllText(path, """
            {
              "runtime": {
                "name": "SteamVR/OpenXR"
              }
            }
            """);

        var name = OpenXrRuntimeProbe.TryParseRuntimeName(path);
        Assert.Equal("SteamVR/OpenXR", name);
    }

    [Fact]
    public void OpenXrProbe_ResolveLabel_FromRuntimeJson()
    {
        var path = Path.Combine(Path.GetTempPath(), $"3dgo-openxr-{Guid.NewGuid()}.json");
        File.WriteAllText(path, """
            {"runtime":{"name":"Meta Quest Link"}}
            """);

        var label = OpenXrRuntimeProbe.TryParseRuntimeName(path);
        Assert.Equal("Meta Quest Link", label);
    }

    [Fact]
    public void OpenXrProbe_ResolveSteamVrRoot_FromRuntimeJson()
    {
        var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"3dgo-steamvr-root-{Guid.NewGuid()}"));
        var runtimePath = Path.Combine(dir.FullName, "runtime.json");
        var steamVrRoot = Path.Combine(dir.FullName, "SteamVR");
        Directory.CreateDirectory(Path.Combine(steamVrRoot, "bin", "win64"));
        File.WriteAllText(Path.Combine(steamVrRoot, "bin", "win64", "vrstartup.exe"), "");
        File.WriteAllText(runtimePath, $$"""
            {
              "runtime": {
                "name": "SteamVR/OpenXR",
                "library_path": "{{Path.Combine(steamVrRoot, "bin", "win64", "openvr_api.dll").Replace("\\", "\\\\")}}"
              }
            }
            """);

        var root = OpenXrRuntimeProbe.TryResolveSteamVrRootFromRuntimeJson(runtimePath);
        Assert.NotNull(root);
        Assert.True(Directory.Exists(root!));
    }

    [Fact]
    public async Task PcvrConnector_ReturnsOpenXrLabel_WhenRuntimeJsonPresent()
    {
        var steamVr = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Steam", "steamapps", "common", "SteamVR");
        if (Directory.Exists(steamVr))
        {
            return;
        }

        var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"3dgo-openxr-dir-{Guid.NewGuid()}"));
        var runtimePath = Path.Combine(dir.FullName, "runtime.json");
        File.WriteAllText(runtimePath, """{"runtime":{"name":"Test Runtime"}}""");

        OpenXrRuntimeProbe.TestRuntimePathResolver = () => runtimePath;
        try
        {
            var connector = new PcvrRuntimeConnector();
            var runtime = await connector.DetectRuntimeAsync();
            Assert.Equal("OpenXR:Test Runtime", runtime);
        }
        finally
        {
            OpenXrRuntimeProbe.TestRuntimePathResolver = null;
        }
    }

    [Fact]
    public async Task SessionProfileService_ListsSavedProfiles()
    {
        var path = Path.Combine(Path.GetTempPath(), $"3dgo-session-{Guid.NewGuid()}.db");
        await using var store = new Infrastructure.Data.SqliteSettingsStore(path);
        await store.InitializeAsync();
        var profiles = new Infrastructure.Updates.SessionProfileService(store);

        await profiles.SaveProfileAsync("LAN");
        await profiles.SaveProfileAsync("Stream");

        var names = await profiles.ListProfileNamesAsync();
        Assert.Contains("LAN", names);
        Assert.Contains("Stream", names);
    }
}
