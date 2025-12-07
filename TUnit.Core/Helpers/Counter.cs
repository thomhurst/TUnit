using System.Diagnostics;

namespace TUnit.Core.Helpers;

/// <summary>
/// Thread-safe counter with event notification.
/// Captures event handler BEFORE state change to prevent race conditions
/// where subscribers miss notifications that occur during subscription.
/// </summary>
[DebuggerDisplay("Count = {CurrentCount}")]
public class Counter
{
    private int _count;

    private volatile EventHandler<int>? _onCountChanged;

    public int Increment()
    {
        // Capture handler BEFORE state change to ensure all subscribers
        // at the time of the change are notified (prevents TOCTOU race)
        var handler = _onCountChanged;
        var newCount = Interlocked.Increment(ref _count);

        handler?.Invoke(this, newCount);

        return newCount;
    }

    public int Decrement()
    {
        // Capture handler BEFORE state change to ensure all subscribers
        // at the time of the change are notified (prevents TOCTOU race)
        var handler = _onCountChanged;
        var newCount = Interlocked.Decrement(ref _count);

        handler?.Invoke(this, newCount);

        return newCount;
    }

    public int Add(int value)
    {
        // Capture handler BEFORE state change to ensure all subscribers
        // at the time of the change are notified (prevents TOCTOU race)
        var handler = _onCountChanged;
        var newCount = Interlocked.Add(ref _count, value);

        handler?.Invoke(this, newCount);

        return newCount;
    }

    public int CurrentCount => Interlocked.CompareExchange(ref _count, 0, 0);

    public event EventHandler<int>? OnCountChanged
    {
        add
        {
            EventHandler<int>? current;
            EventHandler<int>? newHandler;
            do
            {
                current = _onCountChanged;
                newHandler = (EventHandler<int>?)Delegate.Combine(current, value);
            } while (Interlocked.CompareExchange(ref _onCountChanged, newHandler, current) != current);
        }
        remove
        {
            EventHandler<int>? current;
            EventHandler<int>? newHandler;
            do
            {
                current = _onCountChanged;
                newHandler = (EventHandler<int>?)Delegate.Remove(current, value);
            } while (Interlocked.CompareExchange(ref _onCountChanged, newHandler, current) != current);
        }
    }
}
