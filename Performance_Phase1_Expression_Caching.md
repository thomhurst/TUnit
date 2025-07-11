# Phase 1: Expression Caching for Reflection Mode

## Overview
Implement caching for compiled expressions in reflection mode to eliminate redundant expression compilation. This optimization targets the CreateInstanceFactory and CreateTestInvoker methods in ReflectionTestDataCollector.cs.

## Goals
1. Cache compiled expressions by type/method signature
2. Reduce CPU overhead from repeated expression compilation
3. Maintain thread safety and AOT compatibility
4. Keep implementation simple and focused (KISS)

## Current Problem
- Every test method gets its own compiled expression even if signature is identical
- Expression compilation is expensive (involves IL generation)
- No reuse across tests with same method signature

## Design Principles
- **SRP**: Cache is responsible only for storing/retrieving compiled delegates
- **KISS**: Simple concurrent dictionary with composite keys
- **DRY**: Centralize caching logic in dedicated service
- **AOT Compatible**: No dynamic code generation beyond existing Expression.Compile()

## Implementation Plan

### Step 1: Create Expression Cache Service
Create `TUnit.Engine/Services/ExpressionCacheService.cs`:

```csharp
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
```

### Step 2: Refactor CreateInstanceFactory
Move expression compilation to separate method and use cache:

```csharp
// In ReflectionTestDataCollector.cs
private static readonly ExpressionCacheService _expressionCache = new();

private static Func<object?[], object>? CreateInstanceFactory(Type testClass)
{
    var constructors = testClass.GetConstructors();
    if (constructors.Length == 0)
    {
        return null;
    }

    var ctor = constructors.FirstOrDefault(c => c.GetParameters().Length == 0)
               ?? constructors.First();

    return _expressionCache.GetOrCreateInstanceFactory(ctor, CompileInstanceFactory);
}

private static Func<object?[], object> CompileInstanceFactory(ConstructorInfo ctor)
{
    var parameters = ctor.GetParameters();
    var paramExpr = Expression.Parameter(typeof(object[]), "args");

    var argExpressions = new Expression[parameters.Length];
    for (var i = 0; i < parameters.Length; i++)
    {
        var indexExpr = Expression.ArrayIndex(paramExpr, Expression.Constant(i));
        var convertExpr = Expression.Convert(indexExpr, parameters[i].ParameterType);
        argExpressions[i] = convertExpr;
    }

    var newExpr = Expression.New(ctor, argExpressions);
    var lambdaExpr = Expression.Lambda<Func<object?[], object>>(
        Expression.Convert(newExpr, typeof(object)),
        paramExpr);

    return lambdaExpr.Compile();
}
```

### Step 3: Refactor CreateTestInvoker
Similar approach for test invokers:

```csharp
private static Func<object, object?[], Task>? CreateTestInvoker(Type testClass, MethodInfo testMethod)
{
    return _expressionCache.GetOrCreateTestInvoker(testClass, testMethod, 
        key => CompileTestInvoker(key.Item1, key.Item2));
}

private static Func<object, object?[], Task> CompileTestInvoker(Type testClass, MethodInfo testMethod)
{
    // Move existing CreateTestInvoker body here
    var instanceParam = Expression.Parameter(typeof(object), "instance");
    var argsParam = Expression.Parameter(typeof(object[]), "args");
    
    // ... rest of existing implementation ...
    
    return lambda.Compile();
}
```

### Step 4: Add Diagnostics (Optional)
Add logging to track cache effectiveness:

```csharp
// In DiscoverTestsInAssembly after processing
if (VerbosityService.IsEnabled(Verbosity.Diagnostic))
{
    var stats = _expressionCache.GetCacheStatistics();
    Console.WriteLine($"Expression cache stats - Factories: {stats.InstanceFactories}, Invokers: {stats.TestInvokers}");
}
```

## Testing Strategy
1. Verify cache hit rates with duplicate test signatures
2. Ensure thread safety with parallel test discovery
3. Measure performance improvement with large test suites
4. Validate no behavior changes in test execution

## Success Metrics
- 50-90% cache hit rate for typical test suites
- 20-40% reduction in discovery time for reflection mode
- Zero functional regressions

## Risks and Mitigations
- **Risk**: Memory growth from cache
  - **Mitigation**: Cache size is bounded by unique constructors/methods
- **Risk**: Thread contention on cache
  - **Mitigation**: ConcurrentDictionary handles this efficiently

## AOT Compatibility Notes
- No new dynamic code generation
- Reuses existing Expression.Compile() calls
- Cache keys use existing reflection types
- No runtime type generation

## Next Steps
After implementation:
1. Benchmark discovery performance
2. Monitor cache statistics in real projects
3. Consider adding cache size limits if needed
4. Move to Phase 2: Streaming Discovery