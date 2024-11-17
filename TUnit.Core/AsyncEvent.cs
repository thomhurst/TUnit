using System.Runtime.ExceptionServices;

namespace TUnit.Core;

public class AsyncEvent<TEventArgs>
{
    private readonly List<Func<object, TEventArgs, Task>> _invocationList;
    private readonly Lock _locker;

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

    public async ValueTask InvokeAsync(object sender, TEventArgs eventArgs)
    {
        List<Func<object, TEventArgs, Task>> tmpInvocationList;
        
        lock (_locker)
        {
            tmpInvocationList = [.._invocationList];
        }

        var exceptions = new List<Exception>();
        
        foreach (var callback in tmpInvocationList)
        {
            try
            {
                await callback(sender, eventArgs);
            }
            catch (Exception e)
            {
                exceptions.Add(e);
            }
        }

        if (exceptions.Count == 1)
        {
            ExceptionDispatchInfo.Capture(exceptions[0]).Throw();
        }

        if (exceptions.Count > 1)
        {
            throw new AggregateException(exceptions);
        }
    }
    
    public void Unregister() => _invocationList.Clear();
}