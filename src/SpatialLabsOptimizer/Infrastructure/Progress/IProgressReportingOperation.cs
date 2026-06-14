using SpatialLabsOptimizer.Application.Progress;

namespace SpatialLabsOptimizer.Infrastructure.Progress;

public interface IProgressReportingOperation
{
    string OperationId { get; }
    OperationCategory Category { get; }
    event EventHandler<OperationProgressReport>? ProgressChanged;
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
