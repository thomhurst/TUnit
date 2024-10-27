using System.Diagnostics;

namespace TUnit.Engine.Services;

[DebuggerDisplay("Count = {CurrentCount}")]
public class Counter
{
#if NET9_0_OR_GREATER
    private readonly Lock _locker = new();
#else
    private readonly object _locker = new();
#endif
    
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