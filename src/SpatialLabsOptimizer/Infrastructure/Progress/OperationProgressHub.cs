namespace SpatialLabsOptimizer.Infrastructure.Progress;

public sealed class OperationProgressHub
{
    public event EventHandler<OperationProgressReport>? ProgressPublished;

    public void Publish(OperationProgressReport report)
    {
        ProgressPublished?.Invoke(this, report);
    }
}
