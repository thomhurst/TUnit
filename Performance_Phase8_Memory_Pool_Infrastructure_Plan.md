# Phase 8: Memory Pool Infrastructure

## Overview
TUnit currently lacks a memory pooling infrastructure, leading to excessive allocations for frequently created objects like StringBuilders, collections, and buffers. This phase implements a comprehensive pooling system to reduce GC pressure.

## Problem Statement
- No StringBuilder reuse across the framework
- Frequent buffer allocations for I/O operations
- Collection allocations without pooling
- High GC pressure in test-heavy scenarios

## Solution Design

### 1. Core Pool Infrastructure
Create a centralized pooling system that can be used throughout TUnit.

**Key Components:**
- Generic ObjectPool<T> base class
- Thread-safe rent/return operations
- Configurable pool size limits
- Automatic cleanup and reset

### 2. StringBuilder Pool
Dedicated pool for StringBuilder instances with intelligent capacity management.

**Features:**
- Thread-local fast path
- Capacity buckets (small: 256, medium: 1024, large: 4096)
- Automatic trimming of oversized instances
- Clear on return

### 3. ArrayPool Integration
Leverage System.Buffers.ArrayPool for byte[] and char[] arrays.

**Usage Areas:**
- Console output buffers
- String formatting buffers
- Temporary data storage

### 4. Collection Pools
Pooling for frequently used collection types.

**Supported Types:**
- List<T> with capacity management
- Dictionary<TKey, TValue>
- HashSet<T>
- Custom collection wrappers

## Implementation Components

### 1. IPoolable Interface
```csharp
internal interface IPoolable
{
    void Reset();
    bool CanReturn { get; }
}
```

### 2. ObjectPool<T> Base Class
```csharp
internal class ObjectPool<T> where T : class
{
    private readonly ConcurrentBag<T> _pool;
    private readonly Func<T> _factory;
    private readonly Action<T> _reset;
    private readonly int _maxSize;
    
    public T Rent();
    public void Return(T item);
}
```

### 3. StringBuilderPool
```csharp
internal static class StringBuilderPool
{
    private static readonly ObjectPool<StringBuilder>[] _pools;
    
    public static StringBuilder Rent(int capacity = 256);
    public static void Return(StringBuilder sb);
}
```

### 4. PooledList<T>
```csharp
internal class PooledList<T> : List<T>, IDisposable
{
    private static readonly ObjectPool<PooledList<T>> Pool;
    
    public static PooledList<T> Rent(int capacity = 0);
    public void Dispose(); // Returns to pool
}
```

## Integration Points

1. **ConsoleInterceptor**
   - Use StringBuilderPool for formatting
   - ArrayPool for output buffers

2. **TestExtensions**
   - PooledList for temporary collections
   - Reuse property arrays

3. **Event System**
   - Pool event argument objects
   - Reuse notification objects

4. **Test Discovery**
   - Pool temporary collections during discovery
   - Reuse filter lists

## Performance Targets
- 50-70% reduction in StringBuilder allocations
- 30-40% reduction in collection allocations
- 15-25% overall GC pressure reduction
- < 1μs pool operation overhead

## Implementation Steps

1. **Create Core Infrastructure**
   - Implement ObjectPool<T> base class
   - Add IPoolable interface
   - Create pool statistics tracking

2. **Implement StringBuilder Pool**
   - Create capacity-based pools
   - Add thread-local optimization
   - Implement automatic reset

3. **Add Collection Pools**
   - Implement PooledList<T>
   - Add PooledDictionary<TKey, TValue>
   - Create disposal patterns

4. **Integrate ArrayPool**
   - Wrap System.Buffers.ArrayPool
   - Add size buckets
   - Implement safe return patterns

5. **Update Existing Code**
   - Replace new StringBuilder() with pool rentals
   - Convert temporary lists to pooled versions
   - Add using statements for automatic returns

## Testing Strategy
1. Pool correctness tests (proper reset, no shared state)
2. Concurrency tests for thread safety
3. Memory leak detection
4. Performance benchmarks
5. Integration tests with existing code

## Risk Mitigation
- Careful state reset to prevent data leaks
- Pool size limits to prevent memory bloat
- Diagnostic counters for monitoring
- Fallback to non-pooled for edge cases

## Success Criteria
- 50%+ reduction in Gen 0 collections
- Measurable reduction in allocation rate
- No increase in memory footprint
- Zero state leakage between uses
- Improved test execution throughput