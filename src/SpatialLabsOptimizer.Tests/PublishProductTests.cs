namespace SpatialLabsOptimizer.Tests;

public class PublishProductTests
{
    [Fact]
    public void ElevatedHelper_BuildOutput_ExistsAfterSolutionBuild()
    {
        var helper = TestPaths.FindElevatedHelperBuildOutput();
        Assert.True(File.Exists(helper));
    }
}
