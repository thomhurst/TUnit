# Phase 10: Async/Await Fixes

## Overview
TUnit contains several instances of synchronous blocking operations (.Result and .Wait()) that can cause thread pool starvation and deadlocks. This phase converts these to proper async/await patterns for better performance and reliability.

## Problem Statement
- .Result and .Wait() block thread pool threads
- Risk of deadlocks in certain contexts
- Inefficient thread utilization
- Potential for thread pool starvation

## Anti-patterns to Fix

### 1. Task.Result
```csharp
// BAD: Blocks thread
var result = SomeAsyncMethod().Result;

// GOOD: Async all the way
var result = await SomeAsyncMethod();
```

### 2. Task.Wait()
```csharp
// BAD: Blocks thread
SomeAsyncMethod().Wait();

// GOOD: Async all the way
await SomeAsyncMethod();
```

### 3. Task.WaitAll/WaitAny
```csharp
// BAD: Blocks thread
Task.WaitAll(task1, task2, task3);

// GOOD: Async all the way
await Task.WhenAll(task1, task2, task3);
```

## Identified Locations

### Critical Files to Review
Based on grep results, these files need investigation:
1. Test execution paths
2. Discovery services
3. Event handling
4. Hook orchestration
5. Parallel execution coordinators

### Common Patterns to Fix

#### Pattern 1: Sync-over-async in constructors
```csharp
// Before
public MyClass()
{
    _data = LoadDataAsync().Result;
}

// After
public MyClass()
{
    // Move to factory method
}

public static async Task<MyClass> CreateAsync()
{
    var instance = new MyClass();
    instance._data = await LoadDataAsync();
    return instance;
}
```

#### Pattern 2: Sync entry points
```csharp
// Before
public void RunTests()
{
    RunTestsAsync().Wait();
}

// After
public async Task RunTestsAsync()
{
    await RunTestsInternalAsync();
}
```

#### Pattern 3: Event handlers
```csharp
// Before
public void OnTestComplete(TestContext context)
{
    ProcessResultsAsync(context).Wait();
}

// After
public async Task OnTestCompleteAsync(TestContext context)
{
    await ProcessResultsAsync(context);
}
```

## Implementation Strategy

### 1. Bottom-up Approach
Start with leaf methods and work up the call stack:
- Identify all .Result/.Wait() calls
- Convert innermost methods first
- Propagate async up the call chain
- Update interfaces as needed

### 2. Interface Updates
Some interfaces may need async versions:
```csharp
// Add async version alongside sync
public interface ITestExecutor
{
    void Execute(Test test);
    Task ExecuteAsync(Test test, CancellationToken ct = default);
}
```

### 3. Compatibility Considerations
- Maintain backward compatibility where possible
- Use adapter pattern for legacy interfaces
- Provide sync wrappers only at API boundaries
- Document breaking changes clearly

### 4. Special Cases

#### ConfigureAwait Usage
```csharp
// Use ConfigureAwait(false) in library code
await SomeMethodAsync().ConfigureAwait(false);
```

#### ValueTask for Hot Paths
```csharp
// Use ValueTask for frequently called methods
public ValueTask<bool> IsCachedAsync(string key)
{
    if (_cache.TryGetValue(key, out var value))
        return new ValueTask<bool>(true);
    
    return new ValueTask<bool>(CheckRemoteAsync(key));
}
```

## Files to Update

### Priority 1 (Core Execution)
- SingleTestExecutor.cs
- TestExecutor.cs
- HookOrchestrator.cs
- ParallelExecutor.cs

### Priority 2 (Discovery)
- TestDiscoveryService.cs
- TestFinder.cs
- MetadataBuilder.cs

### Priority 3 (Infrastructure)
- EventSystem.cs
- ConsoleCapture.cs
- ResultAggregator.cs

## Performance Benefits
- Better thread pool utilization
- Reduced risk of deadlocks
- Lower latency for I/O operations
- Improved scalability
- More responsive test execution

## Testing Strategy
1. Identify all blocking calls via static analysis
2. Unit test async behavior
3. Stress test for deadlocks
4. Performance comparison before/after
5. Compatibility testing

## Risk Mitigation
- Careful review of each change
- Maintain sync wrappers at public boundaries
- Extensive testing for deadlocks
- Gradual rollout with monitoring
- Clear documentation of changes

## Success Criteria
- Zero .Result/.Wait() in core execution paths
- No deadlocks under stress testing
- Improved thread pool metrics
- Better async performance
- Maintained API compatibility