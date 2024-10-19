namespace TUnit.Core;

public class AsyncEvent<TEventArgs>
{
    private readonly List<Func<object, TEventArgs, Task>> _invocationList;
    #if NET9_0_OR_GREATER
    private readonly Lock _locker;
    #else
    private readonly object _locker;
    #endif

    private AsyncEvent()
    {
        _invocationList = [];
        _locker = new();
    }

    public static AsyncEvent<TEventArgs> operator +(
        AsyncEvent<TEventArgs>? e, Func<object, TEventArgs, Task> callback)
    {
        if (callback == null)
        {
            throw new NullReferenceException("callback is null");
        }

        //Note: Thread safety issue- if two threads register to the same event (on the first time, i.e when it is null)
        //they could get a different instance, so whoever was first will be overridden.
        //A solution for that would be to switch to a public constructor and use it, but then we'll 'lose' the similar syntax to c# events             
        if (e == null)
        {
            e = new AsyncEvent<TEventArgs>();
        }

        lock (e._locker)
        {
            e._invocationList.Add(callback);
        }
        
        return e;
    }

    public static AsyncEvent<TEventArgs>? operator -(
        AsyncEvent<TEventArgs>? e, Func<object, TEventArgs, Task> callback)
    {
        if (callback == null)
        {
            throw new NullReferenceException("callback is null");
        }

        if (e == null)
        {
            return null;
        }

        lock (e._locker)
        {
            e._invocationList.Remove(callback);
        }
        
        return e;
    }

    public async Task InvokeAsync(object sender, TEventArgs eventArgs)
    {
        List<Func<object, TEventArgs, Task>> tmpInvocationList;
        
        lock (_locker)
        {
            tmpInvocationList = [.._invocationList];
        }

        foreach (var callback in tmpInvocationList)
        {
            //Assuming we want a serial invocation, for a parallel invocation we can use Task.WhenAll instead
            await callback(sender, eventArgs);
        }
    }
}