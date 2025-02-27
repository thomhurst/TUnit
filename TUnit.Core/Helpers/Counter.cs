using System.Diagnostics;

namespace TUnit.Core.Helpers;

[DebuggerDisplay("Count = {CurrentCount}")]
public class Counter
{
    private readonly Lock _locker = new();

    private int _count;

    public int Increment()
    {
        lock (_locker)
        {
            _count++;
            OnCountChanged?.Invoke(this, _count);
            return _count;
        }
    }

    public int Decrement()
    {
        lock (_locker)
        {
            _count--;
            OnCountChanged?.Invoke(this, _count);
            return _count;
        }
    }

    public int CurrentCount
    {
        get
        {
            lock (_locker)
            {
                return _count;
            }
        }
    }

    public EventHandler<int>? OnCountChanged;
}