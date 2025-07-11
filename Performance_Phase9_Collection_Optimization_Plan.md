# Phase 9: Collection Optimization

## Overview
TUnit has 34 files with unnecessary ToArray()/ToList() materializations that create allocations in hot paths. This phase optimizes collection usage by deferring execution, using appropriate collection types, and pre-sizing when possible.

## Problem Statement
- Excessive ToArray()/ToList() calls force immediate materialization
- Missing opportunities for deferred execution with IEnumerable
- Collections created without capacity hints
- Unnecessary intermediate collections

## Identified Hot Spots

### Critical Files (High Impact)
1. **TestExtensions.cs**
   - Line 29: `.ToArray()` for parameter types
   - Line 48: Array spread creating new collections
   - Called for every test node creation

2. **EventReceiverCache.cs**
   - Line 57: Unnecessary `.ToArray()` conversion
   - Could store as IEnumerable

3. **TestBuilder.cs**
   - Multiple ToList() calls on test collections
   - Could use deferred execution

### Solution Patterns

### 1. Deferred Execution
Replace immediate materialization with lazy evaluation.

**Before:**
```csharp
var items = collection.Where(x => x.IsValid).ToList();
foreach (var item in items) { /* process */ }
```

**After:**
```csharp
var items = collection.Where(x => x.IsValid);
foreach (var item in items) { /* process */ }
```

### 2. Pre-sized Collections
When materialization is necessary, pre-size collections.

**Before:**
```csharp
var list = new List<T>();
foreach (var item in source) list.Add(item);
```

**After:**
```csharp
var list = new List<T>(source.Count());
list.AddRange(source);
```

### 3. Array Pool Usage
For temporary arrays, use ArrayPool.

**Before:**
```csharp
var array = items.ToArray();
// use array
```

**After:**
```csharp
var array = ArrayPool<T>.Shared.Rent(items.Count());
try {
    items.CopyTo(array, 0);
    // use array
} finally {
    ArrayPool<T>.Shared.Return(array);
}
```

### 4. ReadOnly Collections
Use IReadOnlyList/IReadOnlyCollection where modification isn't needed.

**Before:**
```csharp
public List<T> GetItems() => _items.ToList();
```

**After:**
```csharp
public IReadOnlyList<T> GetItems() => _items;
```

## Implementation Strategy

### Phase 9A: Analysis and Categorization
1. Scan all 34 files with ToArray()/ToList()
2. Categorize by:
   - Required (must materialize)
   - Deferrable (can use IEnumerable)
   - Poolable (temporary usage)
   - Optimizable (can pre-size)

### Phase 9B: Safe Refactoring
1. **Low-risk changes first:**
   - Local variable materializations
   - Private method returns
   - Test-only code

2. **Medium-risk changes:**
   - Internal API changes
   - Protected members
   - Cross-assembly calls

3. **High-risk changes:**
   - Public API modifications
   - Serialization boundaries
   - External interfaces

### Phase 9C: Advanced Optimizations
1. **Custom Enumerables:**
   - Implement yield-based enumerators
   - Avoid intermediate collections

2. **Span Usage:**
   - Replace array operations with Span<T>
   - Stack-allocated collections for small sets

3. **Collection Expressions (C# 12):**
   - Use spread operators efficiently
   - Leverage compiler optimizations

## Specific Optimizations

### 1. TestExtensions.ToTestNode()
```csharp
// Before
ParameterTypeFullNames: testDetails.TestMethodParameterTypes?.Select(x => x.FullName!).ToArray() ?? []

// After  
ParameterTypeFullNames: testDetails.TestMethodParameterTypes?.Count > 0 
    ? CreateParameterArray(testDetails.TestMethodParameterTypes)
    : Array.Empty<string>()

// Helper
private static string[] CreateParameterArray(IReadOnlyList<Type> types)
{
    var array = new string[types.Count];
    for (int i = 0; i < types.Count; i++)
        array[i] = types[i].FullName!;
    return array;
}
```

### 2. EventReceiverCache
```csharp
// Before
return receivers.Cast<object>().ToArray();

// After
return receivers; // Store as IEnumerable<IEventReceiver>
```

### 3. Collection Properties
```csharp
// Before
public List<Test> Tests => _tests.ToList();

// After
public IReadOnlyList<Test> Tests => _tests;
```

## Performance Targets
- 70-80% reduction in ToArray()/ToList() calls
- 20-30% memory allocation reduction
- 15-20% improvement in discovery/execution
- Zero public API breaking changes

## Testing Strategy
1. Benchmark before/after each change
2. Memory profiling for allocation reduction
3. Regression tests for functionality
4. API compatibility tests
5. Performance tests with large test suites

## Risk Mitigation
- Careful analysis of each usage
- Preserve semantics (ordering, mutability)
- Gradual rollout with measurements
- Maintain backward compatibility
- Document any behavior changes

## Success Criteria
- Significant reduction in collection allocations
- No functional regressions
- Improved performance metrics
- Cleaner, more efficient code
- Better memory usage patterns