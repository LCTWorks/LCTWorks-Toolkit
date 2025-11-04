using System.Collections.Concurrent;

namespace LCTWorks.Core;

public partial class ScheduleTimer : IDisposable
{
    private readonly ConcurrentBag<TimeSpan> _checkPoints = [];
    private readonly Lock _gate = new();
    private TimeSpan _cooldown = TimeSpan.FromMinutes(5);
    private bool _disposed;
    private bool _enabled;
    private TimeSpan _interval = TimeSpan.FromSeconds(50);
    private TimeSpan _lastExecution = TimeSpan.Zero;
    private Timer? _timer;

    public event EventHandler? Tick;

    public TimeSpan CheckInterval
    {
        get => _interval;
        set
        {
            if (value <= TimeSpan.Zero)
            {
                value = TimeSpan.FromMilliseconds(1);
            }
            _interval = value;
            if (_enabled && _timer is not null)
            {
                _timer.Change(_interval, _interval);
            }
        }
    }

    public TimeSpan Cooldown
    {
        get => _cooldown;
        set
        {
            if (value < TimeSpan.Zero)
            {
                value = TimeSpan.Zero;
            }
            _cooldown = value;
        }
    }

    public bool IsRunning => _enabled;

    public void AddCheckPoint(TimeSpan time)
    {
        lock (_gate)
        {
            if (!_checkPoints.Contains(time))
            {
                _checkPoints.Add(time);
            }
        }
    }

    public void ClearCheckPoints()
    {
        lock (_gate)
        {
            _checkPoints.Clear();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        Stop();
        _timer?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(ScheduleTimer));

        if (_enabled)
        {
            return;
        }

        _enabled = true;
        _timer ??= new Timer(InternalCallback, null, _interval, _interval);
        _timer.Change(_interval, _interval);
    }

    public void Stop()
    {
        if (!_enabled) return;
        _enabled = false;
        _timer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    private void InternalCallback(object? state)
    {
        if (!_enabled || Tick is null)
        {
            return;
        }

        var currentTime = DateTime.Now.TimeOfDay;
        if (currentTime - _lastExecution < _cooldown)
        {
            return;
        }

        foreach (var tickTime in _checkPoints)
        {
            if (currentTime.Hours == tickTime.Hours && currentTime.Minutes == tickTime.Minutes)
            {
                _lastExecution = currentTime;
                try
                {
                    // Fire on the thread-pool (no UI dependency)
                    Tick?.Invoke(this, EventArgs.Empty);
                }
                catch
                {
                    // Swallow to avoid timer termination.
                }
                break;
            }
        }
    }
}