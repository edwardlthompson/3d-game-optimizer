namespace SpatialLabsOptimizer.Infrastructure.Launch.Coexistence;

public sealed class ExternalToolCoexistenceService
{
    private static readonly string[] TrainerProcesses = ["WeMod", "Wand"];
    private static readonly string[] ModManagerProcesses = ["Vortex", "ModOrganizer"];

    private readonly IRunningProcessProbe _probe;

    public ExternalToolCoexistenceService(IRunningProcessProbe probe)
    {
        _probe = probe;
    }

    public IReadOnlyList<string> GetRunningTrainers() =>
        _probe.GetRunningFrom(TrainerProcesses);

    public IReadOnlyList<string> GetRunningModManagers() =>
        _probe.GetRunningFrom(ModManagerProcesses);

    public IReadOnlyList<string> GetAllRunningExternalTools()
    {
        var tools = new List<string>();
        tools.AddRange(GetRunningTrainers());
        tools.AddRange(GetRunningModManagers());
        return tools;
    }

    public (bool ShouldBlock, LaunchContext Context) Evaluate(
        bool trainerCoexistenceEnabled,
        bool modManagerCoexistenceEnabled)
    {
        var trainers = GetRunningTrainers();
        var modManagers = GetRunningModManagers();
        var detected = trainers.Concat(modManagers).ToList();

        if (trainers.Count > 0 && !trainerCoexistenceEnabled)
        {
            return (true, new LaunchContext(CoexistenceLaunchPolicy.Block, detected));
        }

        if (modManagers.Count > 0 && !modManagerCoexistenceEnabled)
        {
            return (true, new LaunchContext(CoexistenceLaunchPolicy.Block, detected));
        }

        if (detected.Count > 0)
        {
            return (false, new LaunchContext(CoexistenceLaunchPolicy.GameFirst, detected));
        }

        return (false, LaunchContext.Standard);
    }
}
