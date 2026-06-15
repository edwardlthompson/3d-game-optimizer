namespace SpatialLabsOptimizer.Infrastructure;

public static class UiThreadDispatcher
{
    public static Action<Action>? Enqueue { get; set; }

    public static void Run(Action action)
    {
        var enqueue = Enqueue;
        if (enqueue is not null)
        {
            enqueue(action);
            return;
        }

        action();
    }
}
