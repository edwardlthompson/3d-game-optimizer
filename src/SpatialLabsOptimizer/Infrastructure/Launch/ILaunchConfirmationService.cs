using SpatialLabsOptimizer.Infrastructure.Launch;

namespace SpatialLabsOptimizer.Infrastructure.Launch;

/// <summary>UI confirmation before Play in 3D mutates configs or launches.</summary>
public interface ILaunchConfirmationService
{
    /// <returns>true to continue launch; false to cancel.</returns>
    Task<bool> ConfirmAsync(LaunchPreviewSummary summary, CancellationToken cancellationToken = default);
}
