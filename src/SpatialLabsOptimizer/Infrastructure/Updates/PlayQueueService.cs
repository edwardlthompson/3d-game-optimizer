namespace SpatialLabsOptimizer.Infrastructure.Updates;

public sealed class PlayQueueService
{
    private readonly Queue<int> _queue = new();

    public void Enqueue(int appId) => _queue.Enqueue(appId);

    public bool TryDequeue(out int appId) => _queue.TryDequeue(out appId);

    public int Count => _queue.Count;

    public IReadOnlyList<int> Snapshot() => _queue.ToList();
}
