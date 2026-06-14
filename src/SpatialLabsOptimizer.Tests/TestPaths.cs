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
    public static string FindElevatedHelperBuildOutput()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir is not null)
        {
            foreach (var config in new[] { "Debug", "Release" })
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

        throw new InvalidOperationException("ElevatedHelper.exe not found — run dotnet build first");
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
