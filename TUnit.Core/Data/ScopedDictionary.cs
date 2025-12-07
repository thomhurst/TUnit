namespace TUnit.Core.Data;

public class ScopedDictionary<TScope>
    where TScope : notnull
{
    private readonly ThreadSafeDictionary<TScope, ThreadSafeDictionary<Type, object?>> _scopedContainers = new();

    public object? GetOrCreate(TScope scope, Type type, Func<Type, object?> factory)
    {
        var innerDictionary = _scopedContainers.GetOrAdd(scope, static _ => new ThreadSafeDictionary<Type, object?>());

        return innerDictionary.GetOrAdd(type, factory);
    }
}
