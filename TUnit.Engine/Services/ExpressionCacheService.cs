using System.Collections.Concurrent;
using System.Reflection;

namespace TUnit.Engine.Services;

/// <summary>
/// Thread-safe cache for compiled expression delegates
/// </summary>
internal sealed class ExpressionCacheService
{
    // Composite key for instance factories (ConstructorInfo)
    private readonly ConcurrentDictionary<ConstructorInfo, Func<object?[], object>> 
        _instanceFactoryCache = new();
    
    // Composite key for test invokers (Type + MethodInfo)
    private readonly ConcurrentDictionary<(Type declaringType, MethodInfo method), Func<object, object?[], Task>> 
        _testInvokerCache = new();
    
    public Func<object?[], object> GetOrCreateInstanceFactory(
        ConstructorInfo constructor,
        Func<ConstructorInfo, Func<object?[], object>> factory)
    {
        return _instanceFactoryCache.GetOrAdd(constructor, factory);
    }
    
    public Func<object, object?[], Task> GetOrCreateTestInvoker(
        Type declaringType,
        MethodInfo method,
        Func<(Type, MethodInfo), Func<object, object?[], Task>> factory)
    {
        return _testInvokerCache.GetOrAdd((declaringType, method), factory);
    }
    
    // Optional: Add cache statistics for monitoring
    public (int InstanceFactories, int TestInvokers) GetCacheStatistics()
    {
        return (_instanceFactoryCache.Count, _testInvokerCache.Count);
    }
}