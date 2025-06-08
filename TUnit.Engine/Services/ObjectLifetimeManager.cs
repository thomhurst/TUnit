using System.Runtime.CompilerServices;
using TUnit.Core.Helpers;

namespace TUnit.Engine.Services;

internal class ObjectLifetimeManager(Disposer disposer)
{
    private readonly ConditionalWeakTable<object, Counter> _lifetimeTable = new();

    public void RegisterObject(object? obj)
    {
        if (obj == null)
        {
            return;
        }

        _lifetimeTable.GetOrCreateValue(obj).Increment();
    }

    public ValueTask UnregisterObject(object? obj)
    {
        if (obj == null)
        {
            return default;
        }

        if (_lifetimeTable.TryGetValue(obj, out var counter))
        {
            var count = counter.Decrement();

            if (count == 0)
            {
                _lifetimeTable.Remove(obj);
                return disposer.DisposeAsync(obj);
            }

            if (count < 0)
            {
                throw new InvalidOperationException("Unregistering an object that was not registered.");
            }
        }

        return default;
    }
}
