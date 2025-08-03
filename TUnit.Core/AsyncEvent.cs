using TUnit.Core.Interfaces;

namespace TUnit.Core;

public class AsyncEvent<TEventArgs>
{
    public int Order
    {
        get;
        set
        {
            field = value;

            if (InvocationList.Count > 0)
            {
                InvocationList[^1].Order = field;
            }
        }
    } = int.MaxValue / 2;

    internal List<Invocation> InvocationList { get; } = [];

    private static readonly Lock _newEventLock = new();
    private readonly Lock _locker = new();

    public class Invocation(Func<object, TEventArgs, ValueTask> factory, int order) : IEventReceiver
    {
        public int Order
        {
            get;
            internal set;
        } = order;

        public async ValueTask InvokeAsync(object sender, TEventArgs eventArgs)
        {
            await factory(sender, eventArgs);
        }
    }

    public static AsyncEvent<TEventArgs> operator +(
        AsyncEvent<TEventArgs>? e, Func<object, TEventArgs, ValueTask> callback
        )
    {
        if (callback == null)
        {
            throw new NullReferenceException("callback is null");
        }

        lock (_newEventLock)
        {
            e ??= new AsyncEvent<TEventArgs>();
        }

        lock (e._locker)
        {
            e.InvocationList.Add(new Invocation(callback, e.Order));
            e.Order = int.MaxValue / 2;
        }

        return e;
    }

    public AsyncEvent<TEventArgs> InsertAtFront(Func<object, TEventArgs, ValueTask> callback)
    {
        if (callback == null)
        {
            throw new NullReferenceException("callback is null");
        }

        lock (_locker)
        {
            InvocationList.Insert(0, new Invocation(callback, Order));
            Order = int.MaxValue / 2;
        }

        return this;
    }
}
