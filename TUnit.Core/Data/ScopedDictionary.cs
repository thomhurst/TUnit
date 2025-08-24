using TUnit.Core.Tracking;

namespace TUnit.Core.Data;

using System;
using System.Collections.Generic;
using System.Linq;
using TUnit.Core.Tracking;

public class ScopedDictionary<TScope>
    where TScope : notnull
{
    private readonly GetOnlyDictionary<TScope, GetOnlyDictionary<Type, object?>> _scopedContainers = new();

    public object? GetOrCreate(TScope scope, Type type, Func<Type, object?> factory)
    {
        var innerDictionary = _scopedContainers.GetOrAdd(scope, _ => new GetOnlyDictionary<Type, object?>());

        var obj = innerDictionary.GetOrAdd(type, factory);

        // Mark shared objects so ObjectTracker knows not to dispose them immediately
        if (obj != null)
        {
            ObjectTracker.MarkAsShared(obj);
        }

        return obj;
    }

    public void ClearScope(TScope scope)
    {
        _scopedContainers.Remove(scope);
    }

    public IEnumerable<object?> GetScopeValues(TScope scope)
    {
        if (_scopedContainers.TryGetValue(scope, out var innerDictionary))
        {
            return innerDictionary.Values;
        }
        return Enumerable.Empty<object?>();
    }

}
