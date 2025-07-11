# Phase 4: Object Pooling Implementation

## Overview
Implement object pooling for frequently allocated objects to reduce GC pressure and improve performance. Focus on TestContext, TestNodeUpdateMessage, and other high-frequency allocations.

## Goals
1. Reduce Gen 0/1 garbage collections
2. Lower allocation rate during test execution
3. Maintain thread safety and correctness
4. Keep pool implementation simple and focused

## Current Problem
- TestContext created for every test execution
- Messages allocated for every test state change
- No reuse of temporary objects
- High allocation rate causes GC pressure

## Design Principles
- **SRP**: Each pool manages one type of object
- **KISS**: Use simple concurrent collections
- **DRY**: Generic pool implementation for reuse
- **AOT Compatible**: No runtime type generation

## Implementation Plan

### Step 1: Create Generic Object Pool
Create `TUnit.Engine/Pooling/ObjectPool.cs`:

```csharp
using System.Collections.Concurrent;

namespace TUnit.Engine.Pooling;

/// <summary>
/// Thread-safe object pool with configurable size limits
/// </summary>
internal sealed class ObjectPool<T> where T : class
{
    private readonly ConcurrentBag<T> _pool = new();
    private readonly Func<T> _factory;
    private readonly Action<T>? _reset;
    private readonly int _maxSize;
    private int _currentSize;
    
    public ObjectPool(
        Func<T> factory,
        Action<T>? reset = null,
        int maxSize = Environment.ProcessorCount * 2)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _reset = reset;
        _maxSize = maxSize;
    }
    
    public T Rent()
    {
        if (_pool.TryTake(out var item))
        {
            return item;
        }
        
        // Create new instance if pool is empty
        Interlocked.Increment(ref _currentSize);
        return _factory();
    }
    
    public void Return(T item)
    {
        if (item == null) return;
        
        // Reset the object state
        _reset?.Invoke(item);
        
        // Only return to pool if under size limit
        if (_currentSize <= _maxSize)
        {
            _pool.Add(item);
        }
        else
        {
            Interlocked.Decrement(ref _currentSize);
        }
    }
    
    // Statistics for monitoring
    public (int PooledCount, int TotalCreated) GetStatistics()
    {
        return (_pool.Count, _currentSize);
    }
}

/// <summary>
/// Provides automatic return to pool via IDisposable
/// </summary>
internal readonly struct PooledObject<T> : IDisposable where T : class
{
    private readonly ObjectPool<T> _pool;
    public T Value { get; }
    
    public PooledObject(ObjectPool<T> pool, T value)
    {
        _pool = pool;
        Value = value;
    }
    
    public void Dispose()
    {
        _pool.Return(Value);
    }
}

/// <summary>
/// Extension method for convenient pooled object usage
/// </summary>
internal static class ObjectPoolExtensions
{
    public static PooledObject<T> RentScoped<T>(this ObjectPool<T> pool) where T : class
    {
        return new PooledObject<T>(pool, pool.Rent());
    }
}
```

### Step 2: Create Poolable TestContext
Create reset capability for TestContext:

```csharp
// In TestContext.cs or extension
internal static class TestContextPooling
{
    public static void Reset(this TestContext context)
    {
        // Clear test-specific data
        context.TestDetails = null!;
        context.TestResults = null;
        context.Arguments = null;
        context.SkipReason = null;
        context.Dependencies.Clear();
        context.ObjectBag.Clear();
        
        // Reset timing
        context.TestStart = DateTimeOffset.MinValue;
        context.TestDuration = TimeSpan.Zero;
        
        // Clear any captured context
        context.ClearExecutionContext();
    }
}
```

### Step 3: Create Pool Manager
Create `TUnit.Engine/Pooling/TUnitObjectPools.cs`:

```csharp
namespace TUnit.Engine.Pooling;

/// <summary>
/// Centralized pool management for TUnit objects
/// </summary>
internal sealed class TUnitObjectPools : IDisposable
{
    private static readonly Lazy<TUnitObjectPools> _instance = new(() => new TUnitObjectPools());
    public static TUnitObjectPools Instance => _instance.Value;
    
    // Pools for different object types
    public ObjectPool<TestContext> TestContexts { get; }
    public ObjectPool<TestNodeUpdateMessage> UpdateMessages { get; }
    public ObjectPool<StringBuilder> StringBuilders { get; }
    public ObjectPool<List<object?>> ArgumentLists { get; }
    
    private TUnitObjectPools()
    {
        // Initialize pools with appropriate settings
        TestContexts = new ObjectPool<TestContext>(
            factory: () => new TestContext(),
            reset: ctx => ctx.Reset(),
            maxSize: Environment.ProcessorCount * 4);
            
        UpdateMessages = new ObjectPool<TestNodeUpdateMessage>(
            factory: () => new TestNodeUpdateMessage(),
            reset: msg => msg.Reset(),
            maxSize: Environment.ProcessorCount * 2);
            
        StringBuilders = new ObjectPool<StringBuilder>(
            factory: () => new StringBuilder(256),
            reset: sb => sb.Clear(),
            maxSize: Environment.ProcessorCount);
            
        ArgumentLists = new ObjectPool<List<object?>>(
            factory: () => new List<object?>(8),
            reset: list => list.Clear(),
            maxSize: Environment.ProcessorCount * 2);
    }
    
    public void LogStatistics(ILogger logger)
    {
        var (ctxPooled, ctxTotal) = TestContexts.GetStatistics();
        var (msgPooled, msgTotal) = UpdateMessages.GetStatistics();
        
        logger.LogDebug($"Object Pool Stats - TestContext: {ctxPooled}/{ctxTotal}, Messages: {msgPooled}/{msgTotal}");
    }
    
    public void Dispose()
    {
        // Pools will be GC'd, but we could clear them if needed
    }
}
```

### Step 4: Update Test Execution to Use Pools
Modify SingleTestExecutor:

```csharp
// In SingleTestExecutor.cs
public async Task<TestNodeUpdateMessage> ExecuteTestAsync(
    ExecutableTest test,
    IMessageBus messageBus,
    CancellationToken cancellationToken)
{
    // Rent a TestContext from pool if not provided
    var contextFromPool = test.Context == null;
    if (contextFromPool)
    {
        test.Context = TUnitObjectPools.Instance.TestContexts.Rent();
        InitializeTestContext(test.Context, test);
    }
    
    try
    {
        // Execute test...
        return await ExecuteTestCore(test, messageBus, cancellationToken);
    }
    finally
    {
        // Return context to pool if we allocated it
        if (contextFromPool && test.Context != null)
        {
            TUnitObjectPools.Instance.TestContexts.Return(test.Context);
            test.Context = null;
        }
    }
}

private TestNodeUpdateMessage CreateUpdateMessage(ExecutableTest test)
{
    // Use pooled message
    using var pooledMsg = TUnitObjectPools.Instance.UpdateMessages.RentScoped();
    var message = pooledMsg.Value;
    
    // Populate message
    message.TestNode = test.ToTestNode();
    message.SessionUid = _sessionUid!;
    
    // Clone before returning (message will be returned to pool)
    return message.Clone();
}
```

### Step 5: Add String Building Pool
For test name formatting and other string operations:

```csharp
// Example usage in test name formatting
public static string FormatTestName(string baseName, object?[] arguments)
{
    using var pooledSb = TUnitObjectPools.Instance.StringBuilders.RentScoped();
    var sb = pooledSb.Value;
    
    sb.Append(baseName);
    sb.Append('(');
    
    for (int i = 0; i < arguments.Length; i++)
    {
        if (i > 0) sb.Append(", ");
        sb.Append(FormatArgument(arguments[i]));
    }
    
    sb.Append(')');
    return sb.ToString();
}
```

### Step 6: Add Pool Warmup
Pre-allocate objects during startup:

```csharp
// In TUnitServiceProvider or startup
public static void WarmupPools()
{
    var pools = TUnitObjectPools.Instance;
    
    // Pre-allocate some objects
    var contexts = new TestContext[Environment.ProcessorCount];
    for (int i = 0; i < contexts.Length; i++)
    {
        contexts[i] = pools.TestContexts.Rent();
    }
    
    // Return them to pool
    foreach (var ctx in contexts)
    {
        pools.TestContexts.Return(ctx);
    }
}
```

## Configuration
Allow pool sizes to be configured:

```csharp
public class PoolingOptions
{
    public int TestContextPoolSize { get; set; } = Environment.ProcessorCount * 4;
    public int MessagePoolSize { get; set; } = Environment.ProcessorCount * 2;
    public bool EnablePooling { get; set; } = true;
    public bool EnablePoolingDiagnostics { get; set; } = false;
}
```

## Testing Strategy
1. Verify objects are properly reset before reuse
2. Test pool behavior under contention
3. Measure allocation rate reduction
4. Ensure no object state leakage

## Success Metrics
- 40-60% reduction in Gen 0 collections
- 30-50% reduction in allocation rate
- Stable pool sizes during execution
- No increase in memory usage

## Risks and Mitigations
- **Risk**: Object state not properly cleared
  - **Mitigation**: Comprehensive reset methods and testing
- **Risk**: Memory leaks from pooled objects
  - **Mitigation**: Pool size limits and monitoring
- **Risk**: Contention on pool access
  - **Mitigation**: ConcurrentBag minimizes contention

## AOT Compatibility Notes
- Generic pools with concrete types
- No runtime type generation
- All pool types known at compile time
- No reflection in pool implementation

## Performance Considerations
- Pool access is lock-free (ConcurrentBag)
- Minimal overhead for rent/return
- Reset operations should be fast
- Consider NUMA node affinity for large systems

## Next Steps
After implementation:
1. Profile GC behavior improvements
2. Monitor pool utilization rates
3. Tune pool sizes based on workload
4. Move to Phase 5: Worker Thread Optimization