using System.Diagnostics;

namespace TUnit.Core.Helpers;

[DebuggerDisplay("Count = {CurrentCount}")]
public class Counter
{
    private int _count;

    private volatile EventHandler<int>? _onCountChanged;

    public int Increment()
    {
        var newCount = Interlocked.Increment(ref _count);

        var handler = _onCountChanged;
        handler?.Invoke(this, newCount);

        return newCount;
    }

    public int Decrement()
    {
        var newCount = Interlocked.Decrement(ref _count);

        var handler = _onCountChanged;
        handler?.Invoke(this, newCount);

        return newCount;
    }

    public int Add(int value)
    {
        var newCount = Interlocked.Add(ref _count, value);

        var handler = _onCountChanged;
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
