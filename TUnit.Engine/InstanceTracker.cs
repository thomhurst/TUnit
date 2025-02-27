using System.Collections.Concurrent;
using System.Reflection;
using TUnit.Core.Helpers;

namespace TUnit.Engine;

internal class InstanceTracker
{
    private readonly ConcurrentDictionary<Type, Counter> _perClassType = new();
    
    private readonly ConcurrentDictionary<Assembly, Counter> _perAssembly = new();

    private readonly Counter _totalInstances = new();
    
    public void Register(Type classType)
    {
        foreach (var type in GetTypesIncludingBase(classType))
        {
            _perClassType.GetOrAdd(type, _ => new Counter()).Increment();
        }
        
        _perAssembly.GetOrAdd(classType.Assembly, _ => new Counter()).Increment();

        _totalInstances.Increment();
    }

    public bool IsLastTestForType(Type type)
    {
        var count = _perClassType[type].Decrement();
            
        if (count < 0)
        {
            throw new Exception($"Remaining tests has gone below 0 for Type {type}");
        }

        return count == 0;
    }
    
    public bool IsLastTestForAssembly(Assembly assembly)
    {
        var count = _perAssembly[assembly].Decrement();

        if (count < 0)
        {
            throw new Exception("Remaining tests has gone below 0");
        }

        return count == 0;
    }

    public bool IsLastTest()
    {
        return _totalInstances.Decrement() == 0;
    }

    private static IEnumerable<Type> GetTypesIncludingBase(Type testClassType)
    {
        var type = testClassType;
        
        while (type != null && type != typeof(object))
        {
            yield return type;
            type = type.BaseType;
        }
    }
}