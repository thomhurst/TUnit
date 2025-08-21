using TUnit.Core.Tracking;

namespace TUnit.Core.Data;

public class ScopedDictionary<TScope>
    where TScope : notnull
{
    private readonly GetOnlyDictionary<TScope, GetOnlyDictionary<Type, object?>> _scopedContainers = new();

    public object? GetOrCreate(TScope scope, Type type, Func<Type, object?> factory)
    {
        var innerDictionary = _scopedContainers.GetOrAdd(scope, _ => new GetOnlyDictionary<Type, object?>());

        var obj = innerDictionary.GetOrAdd(type, factory);

        ObjectTracker.OnDisposed(obj, () =>
        {
            innerDictionary.Remove(type);
        });

        return obj;
    }

}
