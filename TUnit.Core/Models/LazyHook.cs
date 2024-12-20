namespace TUnit.Core;

internal class LazyHook<T1, T2>(Func<T1, T2, Task> func)
{
#if NET
    private readonly Lock _lock = new();
#else
    private readonly Backport.System.Threading.Lock _lock = Backport.System.Threading.LockFactory.Create();
#endif
    private Task? _value;

    public Task Value(T1 arg1, T2 arg2)
    {
        lock (_lock)
        {
            return _value ??= func(arg1, arg2);
        }
    }
}