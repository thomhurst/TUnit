using TUnit.Core.Interfaces;

namespace TUnit.Core;

public class AsyncEvent<TEventArgs>
{
    private List<Invocation>? _handlers;

    public class Invocation(Func<object, TEventArgs, ValueTask> factory, int order) : IEventReceiver
    {
        public int Order { get; } = order;

        public async ValueTask InvokeAsync(object sender, TEventArgs eventArgs)
        {
            await factory(sender, eventArgs);
        }
    }

    public void Add(Func<object, TEventArgs, ValueTask> callback, int order = int.MaxValue / 2)
    {
        if (callback == null)
        {
            throw new ArgumentNullException(nameof(callback));
        }

        var invocation = new Invocation(callback, order);
        var insertIndex = FindInsertionIndex(order);
        (_handlers ??= []).Insert(insertIndex, invocation);
    }

    public void AddAt(Func<object, TEventArgs, ValueTask> callback, int index, int order = int.MaxValue / 2)
    {
        if (callback == null)
        {
            throw new ArgumentNullException(nameof(callback));
        }

        var invocation = new Invocation(callback, order);
        var handlers = _handlers ??= [];
        var clampedIndex = index < 0 ? 0 : (index > handlers.Count ? handlers.Count : index);
        handlers.Insert(clampedIndex, invocation);
    }

    public IReadOnlyList<Invocation> InvocationList
    {
        get
        {
            if (_handlers == null)
            {
                return [];
            }

            return _handlers;

        }
    }

    public AsyncEvent<TEventArgs> InsertAtFront(Func<object, TEventArgs, ValueTask> callback)
    {
        AddAt(callback, 0);
        return this;
    }

    public static AsyncEvent<TEventArgs> operator +(
        AsyncEvent<TEventArgs>? e, Func<object, TEventArgs, ValueTask> callback)
    {
        e ??= new AsyncEvent<TEventArgs>();
        e.Add(callback);
        return e;
    }

    private int FindInsertionIndex(int order)
    {
        int left = 0, right = (_handlers ??= []).Count;
        while (left < right)
        {
            var mid = left + (right - left) / 2;
            if (_handlers[mid].Order <= order)
                left = mid + 1;
            else
                right = mid;
        }
        return left;
    }
}
