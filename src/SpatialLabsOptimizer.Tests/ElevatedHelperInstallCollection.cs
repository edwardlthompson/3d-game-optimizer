namespace SpatialLabsOptimizer.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ElevatedHelperInstallCollection : ICollectionFixture<ElevatedHelperInstallFixture>
{
    public const string Name = "ElevatedHelperInstall";
}

public sealed class ElevatedHelperInstallFixture
{
    public ElevatedHelperInstallFixture()
    {
        CleanupTool("reshade");
        CleanupTool("uevr");
    }

    internal static string ToolInstallRoot(string toolId)
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "3d-game-optimizer",
            "tools",
            toolId);
    }

    internal static void CleanupTool(string toolId)
    {
        var root = ToolInstallRoot(toolId);
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
