using SpatialLabsOptimizer.Application.Progress;

namespace SpatialLabsOptimizer.Infrastructure.Progress;

public sealed record OperationProgressReport(
    string OperationId,
    OperationCategory Category,
    string Title,
    string CurrentStep,
    int? StepIndex = null,
    int? TotalSteps = null,
    double? PercentComplete = null,
    long? BytesTransferred = null,
    long? BytesTotal = null,
    string? DetailMessage = null,
    bool IsCancellable = false,
    bool IsComplete = false,
    bool IsFailed = false,
    string? ErrorMessage = null)
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
