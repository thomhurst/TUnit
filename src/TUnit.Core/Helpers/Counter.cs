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

        RaiseEventSafely(handler, newCount);

        return newCount;
    }

    public int Decrement()
    {
        // Capture handler BEFORE state change to ensure all subscribers
        // at the time of the change are notified (prevents TOCTOU race)
        var handler = _onCountChanged;
        var newCount = Interlocked.Decrement(ref _count);

        RaiseEventSafely(handler, newCount);

        return newCount;
    }

    /// <summary>
    /// Adds a value to the counter. Use Increment/Decrement for single-step changes.
    /// </summary>
    /// <param name="value">The value to add (can be positive or negative).</param>
    /// <returns>The new count after the addition.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the resulting count is negative, indicating a logic error.</exception>
    public int Add(int value)
    {
        // Capture handler BEFORE state change to ensure all subscribers
        // at the time of the change are notified (prevents TOCTOU race)
        var handler = _onCountChanged;
        var newCount = Interlocked.Add(ref _count, value);

        // Guard against reference count going negative - indicates a bug in calling code
        if (newCount < 0)
        {
            throw new InvalidOperationException(
                $"Counter went below zero (result: {newCount}). This indicates a bug in the reference counting logic.");
        }

        RaiseEventSafely(handler, newCount);

        return newCount;
    }

    /// <summary>
    /// Raises the event safely, ensuring all subscribers are notified even if some throw exceptions.
    /// Collects all exceptions and throws AggregateException if any occurred.
    /// </summary>
    private void RaiseEventSafely(EventHandler<int>? handler, int newCount)
    {
        if (handler == null)
        {
            return;
        }

        var invocationList = handler.GetInvocationList();
        List<Exception>? exceptions = null;

        foreach (var subscriber in invocationList)
        {
            try
            {
                ((EventHandler<int>)subscriber).Invoke(this, newCount);
            }
            catch (Exception ex)
            {
                exceptions ??= [];
                exceptions.Add(ex);

#if DEBUG
                Debug.WriteLine($"[Counter] Exception in OnCountChanged subscriber: {ex.Message}");
#endif
            }
        }

        // If any subscribers threw, aggregate and rethrow after all are notified
        if (exceptions?.Count > 0)
        {
            throw new AggregateException("One or more OnCountChanged subscribers threw an exception.", exceptions);
        }
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
