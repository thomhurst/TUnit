using System.Collections.Concurrent;
using System.Reflection;

namespace TUnit.Core;

internal static class InstanceTracker
{
    private static readonly object PerClassTypeLock = new();
    private static readonly ConcurrentDictionary<Type, int> PerClassType = new();
    
    private static readonly object PerAssemblyLock = new();
    private static readonly ConcurrentDictionary<Assembly, int> PerAssembly = new();

    private static int TotalInstances;
    
    public static void Register(Type classType)
    {
        foreach (var type in GetTypesIncludingBase(classType))
        {
            var count = PerClassType.GetOrAdd(type, _ => 0);
            PerClassType[type] = count + 1;
        }
        
        lock (PerAssemblyLock)
        {
            var assembly = classType.Assembly;
            var count = PerAssembly.GetOrAdd(assembly, _ => 0);
            PerAssembly[assembly] = count + 1;
        }

        TotalInstances++;
    }

    public static bool IsLastTestForType(Type type)
    {
        lock (PerClassTypeLock)
        {
            var count = PerClassType[type];
            var newCount = count - 1;
            PerClassType[type] = newCount;
            
            if (newCount < 0)
            {
                throw new Exception($"Remaining tests has gone below 0 for Type {type}");
            }

            return newCount == 0;
        }
    }
    
    public static bool IsLastTestForAssembly(Assembly assembly)
    {
        lock (PerAssemblyLock)
        {
            var count = PerAssembly[assembly];
            var newCount = count - 1;
            PerAssembly[assembly] = newCount;

            if (newCount < 0)
            {
                throw new Exception("Remaining tests has gone below 0");
            }

            return newCount == 0;
        }
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

    public static bool IsLastTest()
    {
        return Interlocked.Decrement(ref TotalInstances) == 0;
    }
}