using SpatialLabsOptimizer.Infrastructure.Launch;

namespace SpatialLabsOptimizer.Infrastructure.Launch;

/// <summary>Always-confirm stub for headless tests (no UI).</summary>
public sealed class AlwaysConfirmLaunchConfirmationService : ILaunchConfirmationService
{
    public Task<bool> ConfirmAsync(LaunchPreviewSummary summary, CancellationToken cancellationToken = default)
        => Task.FromResult(true);
}
