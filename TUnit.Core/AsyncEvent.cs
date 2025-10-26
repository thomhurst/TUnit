using System.Collections.Immutable;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Lock-free async event implementation using immutable snapshots for better concurrent performance.
/// </summary>
public class AsyncEvent<TEventArgs>
{
    public int Order
    {
        get;
        set
        {
            field = value;

            var snapshot = _invocationList;
            if (snapshot.Count > 0)
            {
                snapshot[^1].Order = field;
            }
        }
    } = int.MaxValue / 2;

    private volatile ImmutableList<Invocation> _invocationList = ImmutableList<Invocation>.Empty;

    internal ImmutableList<Invocation> InvocationList => _invocationList;

    private static readonly Lock _newEventLock = new();

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

        // Lock-free add using Interlocked.CompareExchange
        var newInvocation = new Invocation(callback, e.Order);
        ImmutableList<Invocation> current, updated;
        do
        {
            current = e._invocationList;
            updated = current.Add(newInvocation);
        }
        while (Interlocked.CompareExchange(ref e._invocationList, updated, current) != current);

        e.Order = int.MaxValue / 2;

        return e;
    }

    public AsyncEvent<TEventArgs> InsertAtFront(Func<object, TEventArgs, ValueTask> callback)
    {
        if (callback == null)
        {
            throw new NullReferenceException("callback is null");
        }

        // Lock-free insert using Interlocked.CompareExchange
        var newInvocation = new Invocation(callback, Order);
        ImmutableList<Invocation> current, updated;
        do
        {
            current = _invocationList;
            updated = current.Insert(0, newInvocation);
        }
        while (Interlocked.CompareExchange(ref _invocationList, updated, current) != current);

        Order = int.MaxValue / 2;

        return this;
    }
}
