namespace SpatialLabsOptimizer.Infrastructure.Displays;

public sealed class DisplayConfigurationChangedEventArgs : EventArgs
{
    public DisplayConfigurationChangedEventArgs(
        IReadOnlyList<DisplayEdidSnapshot> previous,
        IReadOnlyList<DisplayEdidSnapshot> current)
    {
        Previous = previous;
        Current = current;
    }

    public IReadOnlyList<DisplayEdidSnapshot> Previous { get; }
    public IReadOnlyList<DisplayEdidSnapshot> Current { get; }
}

public sealed class DisplayChangeMonitor : IDisposable
{
    private readonly IDisplayEdidProbe _probe;
    private readonly object _gate = new();
    private IReadOnlyList<DisplayEdidSnapshot> _lastSnapshot = Array.Empty<DisplayEdidSnapshot>();
    private Timer? _timer;
    private bool _started;

    public DisplayChangeMonitor(IDisplayEdidProbe probe)
    {
        _probe = probe;
    }

    public event EventHandler<DisplayConfigurationChangedEventArgs>? ConfigurationChanged;

    public void Start(TimeSpan pollInterval)
    {
        lock (_gate)
        {
            if (_started)
            {
                return;
            }

            _lastSnapshot = _probe.GetCurrentSnapshots();
            _timer = new Timer(_ => Poll(), null, pollInterval, pollInterval);
            _started = true;
        }
    }

    public void Stop()
    {
        lock (_gate)
        {
            _timer?.Dispose();
            _timer = null;
            _started = false;
        }
    }

    public bool CheckForChanges()
    {
        var current = _probe.GetCurrentSnapshots();
        IReadOnlyList<DisplayEdidSnapshot> previous;
        lock (_gate)
        {
            previous = _lastSnapshot;
            if (previous.Count == 0)
            {
                _lastSnapshot = current;
                return false;
            }

            if (!HasSnapshotChanged(previous, current))
            {
                return false;
            }

            _lastSnapshot = current;
        }

        ConfigurationChanged?.Invoke(this, new DisplayConfigurationChangedEventArgs(previous, current));
        return true;
    }

    public void Dispose() => Stop();

    private void Poll()
    {
        try
        {
            CheckForChanges();
        }
        catch (Exception)
        {
            // Access denied or WMI unavailable — skip this poll cycle.
        }
    }

    public static bool HasSnapshotChanged(
        IReadOnlyList<DisplayEdidSnapshot> previous,
        IReadOnlyList<DisplayEdidSnapshot> current)
    {
        var prevKeys = previous.Select(s => s.EdidSignature).Order(StringComparer.Ordinal).ToArray();
        var currKeys = current.Select(s => s.EdidSignature).Order(StringComparer.Ordinal).ToArray();
        return !prevKeys.SequenceEqual(currKeys, StringComparer.Ordinal);
    }
}
