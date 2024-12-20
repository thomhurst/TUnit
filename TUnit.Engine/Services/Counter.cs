using System.Diagnostics;

namespace TUnit.Engine.Services;

[DebuggerDisplay("Count = {CurrentCount}")]
public class Counter
{
#if NET
    private readonly Lock _locker = new();
#else
    private readonly Backport.System.Threading.Lock _locker = Backport.System.Threading.LockFactory.Create();
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