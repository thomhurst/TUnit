# Phase 2: Streaming Discovery Architecture

## Overview
Transform test discovery from collection-based to streaming-based using IAsyncEnumerable. This enables tests to start executing while discovery is still in progress, dramatically reducing time-to-first-test.

## Goals
1. Enable test execution to begin before discovery completes
2. Reduce memory usage by not holding all tests in memory
3. Maintain compatibility with dependency resolution
4. Keep changes focused and avoid over-engineering

## Current Problem
- Must discover ALL tests before ANY can execute
- Entire test suite held in memory (List/ConcurrentBag)
- Dependency resolution requires second pass through all tests
- Large test suites have long startup delays

## Design Principles
- **SRP**: Separate discovery streaming from dependency resolution
- **KISS**: Minimal changes to existing interfaces
- **DRY**: Reuse existing test building logic
- **AOT Compatible**: No dynamic types or runtime code generation

## Implementation Strategy
Use a two-stage approach:
1. Stream tests with unresolved dependencies
2. Resolve dependencies on-demand as tests are consumed

## Implementation Plan

### Step 1: Create Streaming Test Discovery Interface
Create `TUnit.Engine/Interfaces/IStreamingTestDiscovery.cs`:

```csharp
namespace TUnit.Engine.Interfaces;

/// <summary>
/// Provides streaming test discovery capabilities
/// </summary>
internal interface IStreamingTestDiscovery
{
    /// <summary>
    /// Discovers tests as a stream, enabling parallel discovery and execution
    /// </summary>
    IAsyncEnumerable<ExecutableTest> DiscoverTestsStreamAsync(
        string testSessionId, 
        CancellationToken cancellationToken = default);
}
```

### Step 2: Create Dependency Resolver Service
Create `TUnit.Engine/Services/TestDependencyResolver.cs`:

```csharp
using System.Collections.Concurrent;

namespace TUnit.Engine.Services;

/// <summary>
/// Resolves test dependencies on-demand during streaming
/// </summary>
internal sealed class TestDependencyResolver
{
    private readonly ConcurrentDictionary<string, ExecutableTest> _testsByName = new();
    private readonly ConcurrentDictionary<string, List<string>> _pendingDependents = new();
    
    public void RegisterTest(ExecutableTest test)
    {
        _testsByName[test.TestId] = test;
        
        // Process any tests waiting for this one
        if (_pendingDependents.TryRemove(test.TestId, out var dependents))
        {
            foreach (var dependentId in dependents)
            {
                if (_testsByName.TryGetValue(dependentId, out var dependent))
                {
                    ResolveDependenciesForTest(dependent);
                }
            }
        }
    }
    
    public bool TryResolveDependencies(ExecutableTest test)
    {
        if (test.Dependencies.Length > 0)
        {
            return true; // Already resolved
        }
        
        return ResolveDependenciesForTest(test);
    }
    
    private bool ResolveDependenciesForTest(ExecutableTest test)
    {
        var dependencies = new List<ExecutableTest>();
        var allResolved = true;
        
        foreach (var dependency in test.Metadata.Dependencies)
        {
            var matchingTests = _testsByName.Values
                .Where(t => dependency.Matches(t.Metadata, test.Metadata))
                .ToList();
                
            if (matchingTests.Count == 0)
            {
                // Dependency not yet discovered, register for notification
                var depKey = dependency.ToString();
                _pendingDependents.AddOrUpdate(depKey,
                    _ => new List<string> { test.TestId },
                    (_, list) => { list.Add(test.TestId); return list; });
                allResolved = false;
            }
            else
            {
                dependencies.AddRange(matchingTests);
            }
        }
        
        if (allResolved)
        {
            test.Dependencies = dependencies
                .Distinct()
                .Where(d => d.TestId != test.TestId)
                .ToArray();
                
            // Update TestContext.Dependencies
            test.Context.Dependencies.Clear();
            foreach (var dep in GetAllDependencies(test, new HashSet<string>()))
            {
                test.Context.Dependencies.Add(dep.Context.TestDetails);
            }
        }
        
        return allResolved;
    }
    
    private IEnumerable<ExecutableTest> GetAllDependencies(
        ExecutableTest test, 
        HashSet<string> visited)
    {
        foreach (var dep in test.Dependencies)
        {
            if (visited.Add(dep.TestId))
            {
                yield return dep;
                foreach (var transitive in GetAllDependencies(dep, visited))
                {
                    yield return transitive;
                }
            }
        }
    }
}
```

### Step 3: Update TestDiscoveryServiceV2
Add streaming support while maintaining backward compatibility:

```csharp
public sealed class TestDiscoveryServiceV2 : IDataProducer, IStreamingTestDiscovery
{
    private readonly TestDependencyResolver _dependencyResolver = new();
    
    // Existing collection-based method for compatibility
    public async Task<IEnumerable<ExecutableTest>> DiscoverTests(string testSessionId)
    {
        // Use streaming internally but collect results
        var tests = new List<ExecutableTest>();
        await foreach (var test in DiscoverTestsStreamAsync(testSessionId))
        {
            tests.Add(test);
        }
        return tests;
    }
    
    // New streaming method
    public async IAsyncEnumerable<ExecutableTest> DiscoverTestsStreamAsync(
        string testSessionId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var test in _testBuilderPipeline.BuildTestsStreamAsync(testSessionId, cancellationToken))
        {
            _dependencyResolver.RegisterTest(test);
            
            // Try to resolve dependencies immediately
            if (!_dependencyResolver.TryResolveDependencies(test) && test.Metadata.Dependencies.Length > 0)
            {
                // Mark as pending if dependencies not ready
                test.State = TestState.Pending;
            }
            
            yield return test;
        }
    }
}
```

### Step 4: Create Streaming Test Executor
Create `TUnit.Engine/Execution/StreamingTestExecutor.cs`:

```csharp
namespace TUnit.Engine.Execution;

/// <summary>
/// Executes tests as they are discovered via streaming
/// </summary>
internal sealed class StreamingTestExecutor
{
    private readonly UnifiedTestExecutor _executor;
    private readonly TestDependencyResolver _dependencyResolver;
    private readonly Channel<ExecutableTest> _readyTests;
    
    public StreamingTestExecutor(
        UnifiedTestExecutor executor,
        TestDependencyResolver dependencyResolver)
    {
        _executor = executor;
        _dependencyResolver = dependencyResolver;
        _readyTests = Channel.CreateUnbounded<ExecutableTest>();
    }
    
    public async Task ExecuteStreamingTests(
        IAsyncEnumerable<ExecutableTest> testStream,
        ITestExecutionFilter? filter,
        IMessageBus messageBus,
        CancellationToken cancellationToken)
    {
        // Start execution pipeline
        var executionTask = ProcessReadyTests(filter, messageBus, cancellationToken);
        
        // Feed tests into pipeline as discovered
        await foreach (var test in testStream.WithCancellation(cancellationToken))
        {
            if (IsTestReady(test))
            {
                await _readyTests.Writer.WriteAsync(test, cancellationToken);
            }
        }
        
        _readyTests.Writer.Complete();
        await executionTask;
    }
    
    private bool IsTestReady(ExecutableTest test)
    {
        // Test is ready if it has no dependencies or all are resolved
        return test.Metadata.Dependencies.Length == 0 || 
               _dependencyResolver.TryResolveDependencies(test);
    }
    
    private async Task ProcessReadyTests(
        ITestExecutionFilter? filter,
        IMessageBus messageBus,
        CancellationToken cancellationToken)
    {
        await foreach (var test in _readyTests.Reader.ReadAllAsync(cancellationToken))
        {
            // Execute using existing executor
            await _executor.ExecuteTests(new[] { test }, filter, messageBus, cancellationToken);
        }
    }
}
```

### Step 5: Update Pipeline Interfaces
Modify `UnifiedTestBuilderPipeline` to support streaming:

```csharp
public async IAsyncEnumerable<ExecutableTest> BuildTestsStreamAsync(
    string testSessionId,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    // Stream from collectors
    await foreach (var metadata in CollectTestsStreamAsync(testSessionId, cancellationToken))
    {
        // Build executable test
        var test = _testBuilder.BuildExecutableTest(metadata, testSessionId);
        
        if (test != null)
        {
            yield return test;
        }
    }
}

private async IAsyncEnumerable<TestMetadata> CollectTestsStreamAsync(
    string testSessionId,
    [EnumeratorCancellation] CancellationToken cancellationToken)
{
    foreach (var collector in _collectors)
    {
        // Check if collector supports streaming
        if (collector is IStreamingTestDataCollector streamingCollector)
        {
            await foreach (var test in streamingCollector.CollectTestsStreamAsync(testSessionId, cancellationToken))
            {
                yield return test;
            }
        }
        else
        {
            // Fall back to collection-based for non-streaming collectors
            var tests = await collector.CollectTestsAsync(testSessionId);
            foreach (var test in tests)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return test;
            }
        }
    }
}
```

## Migration Strategy
1. Add streaming interfaces alongside existing ones
2. Implement streaming in phases (AOT first, then reflection)
3. Switch to streaming by default once stable
4. Deprecate collection-based APIs in future release

## Testing Strategy
1. Verify tests with dependencies still execute correctly
2. Measure time-to-first-test improvement
3. Validate memory usage reduction
4. Ensure no test execution order violations

## Success Metrics
- 80-95% reduction in time-to-first-test
- 40-60% reduction in peak memory usage
- Zero test execution errors from streaming

## Risks and Mitigations
- **Risk**: Complex dependency graphs may deadlock
  - **Mitigation**: Timeout and fallback to collection mode
- **Risk**: Breaking change for extensions
  - **Mitigation**: Maintain backward compatibility layer

## AOT Compatibility Notes
- IAsyncEnumerable is AOT-friendly
- No dynamic dispatch or runtime generation
- Channel<T> is AOT-compatible
- All types known at compile time

## Next Steps
After implementation:
1. Profile memory usage improvements
2. Measure startup time reduction
3. Implement Phase 3: Lazy Data Source Evaluation