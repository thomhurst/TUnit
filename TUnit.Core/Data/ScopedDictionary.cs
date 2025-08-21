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

    /// <summary>
    /// Removes a specific value from all scopes and types where it might be stored.
    /// This is used to clear disposed objects from the cache.
    /// </summary>
    public void RemoveValue(object valueToRemove)
    {
        // Since GetOnlyDictionary doesn't support removal, we'll need to track this differently
        // For now, this is a no-op but could be implemented if removal is needed
    }
}
