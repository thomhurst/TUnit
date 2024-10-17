namespace TUnit.Core;

internal class LazyHook<T1, T2>(Func<T1, T2, Task> func)
{
#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
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