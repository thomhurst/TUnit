using System.Collections.Concurrent;
using TUnit.Core.Helpers;

namespace TUnit.Core.Data;

internal static class ActiveObjectTracker
{
    private static readonly ConcurrentDictionary<object, Counter> _activeObjects = new();
    
    public static void IncrementUsage(object? instance)
    {
        if (instance == null || IsValueTypeOrString(instance))
        {
            return;
        }
        
        var counter = _activeObjects.GetOrAdd(instance, _ => new Counter());
        counter.Increment();
    }
    
    public static void IncrementUsage(IEnumerable<object?> instances)
    {
        foreach (var instance in instances)
        {
            IncrementUsage(instance);
        }
    }
    
    public static bool TryGetCounter(object? instance, out Counter? counter)
    {
        counter = null;
        if (instance == null || IsValueTypeOrString(instance))
        {
            return false;
        }
        
        return _activeObjects.TryGetValue(instance, out counter);
    }
    
    public static void RemoveObject(object instance)
    {
        if (instance != null)
        {
            _activeObjects.TryRemove(instance, out _);
        }
    }
    
    private static bool IsValueTypeOrString(object instance)
    {
        var type = instance.GetType();
        return type.IsValueType || type == typeof(string);
    }
}