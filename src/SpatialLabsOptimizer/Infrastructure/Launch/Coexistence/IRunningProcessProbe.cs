namespace SpatialLabsOptimizer.Infrastructure.Launch.Coexistence;

public interface IRunningProcessProbe
{
    bool IsProcessRunning(string processName);

    IReadOnlyList<string> GetRunningFrom(params string[] processNames);
}

public sealed class RunningProcessProbe : IRunningProcessProbe
{
    public bool IsProcessRunning(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            return false;
        }

        return System.Diagnostics.Process.GetProcessesByName(processName).Length > 0;
    }

    public IReadOnlyList<string> GetRunningFrom(params string[] processNames)
    {
        var running = new List<string>();
        foreach (var name in processNames)
        {
            if (IsProcessRunning(name))
            {
                running.Add(name);
            }
        }

        return running;
    }
}
