using SpatialLabsOptimizer.Infrastructure.Launch.Coexistence;

namespace SpatialLabsOptimizer.Infrastructure.Updates;

public sealed class ModManagerIntegrationService
{
    private readonly ExternalToolCoexistenceService _coexistence;

    public ModManagerIntegrationService(ExternalToolCoexistenceService coexistence)
    {
        _coexistence = coexistence;
    }

    public bool IsModManagerRunning() => _coexistence.GetRunningModManagers().Count > 0;
}
