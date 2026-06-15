using SpatialLabsOptimizer.Infrastructure.Install;

namespace SpatialLabsOptimizer.Tests;

internal sealed class TestHelperLocator : IElevatedHelperLocator
{
    public TestHelperLocator(string helperPath) => HelperPath = helperPath;
    public string HelperPath { get; }
}

internal sealed class StubMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
        => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
}

internal static class TestPaths
{
    public static Infrastructure.Displays.DisplayAutoDetector CreateDisplayAutoDetector()
    {
        var loader = new Infrastructure.Data.JsonDataLoader(FindDataRoot());
        return new Infrastructure.Displays.DisplayAutoDetector(loader, new Infrastructure.Displays.WmiDisplayEdidProbe());
    }

    public static string FindElevatedHelperBuildOutput()
    {
        var found = TryFindElevatedHelperBuildOutput();
        if (found is not null)
        {
            return found;
        }

        BuildElevatedHelperIfNeeded();
        found = TryFindElevatedHelperBuildOutput();
        if (found is not null)
        {
            return found;
        }

        throw new InvalidOperationException("ElevatedHelper.exe not found — run dotnet build SpatialLabsOptimizer.sln");
    }

    private static string? TryFindElevatedHelperBuildOutput()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir is not null)
        {
            foreach (var config in new[] { "Release", "Debug" })
            {
                var candidate = Path.Combine(
                    dir.FullName,
                    "src",
                    "SpatialLabsOptimizer.ElevatedHelper",
                    "bin",
                    config,
                    "net8.0",
                    "SpatialLabsOptimizer.ElevatedHelper.exe");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            dir = dir.Parent;
        }

        return null;
    }

    private static void BuildElevatedHelperIfNeeded()
    {
        var root = FindRepoRoot();
        var project = Path.Combine(root, "src", "SpatialLabsOptimizer.ElevatedHelper", "SpatialLabsOptimizer.ElevatedHelper.csproj");
        if (!File.Exists(project))
        {
            return;
        }

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build \"{project}\" -c Release --verbosity quiet",
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using var process = System.Diagnostics.Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start dotnet build for ElevatedHelper");
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException("dotnet build ElevatedHelper failed — build SpatialLabsOptimizer.sln first");
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "SpatialLabsOptimizer.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("SpatialLabsOptimizer.sln not found");
    }

    public static string FindDataRoot()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "data");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("data folder not found");
    }
}
