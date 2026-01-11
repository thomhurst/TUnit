# Lazy Test Materialization Design

## Problem Statement

TUnit's test discovery is ~9% slower than MSTest for single/few test scenarios. Profiling reveals the bottleneck is **eager materialization**: every test creates a full `TestMetadata` object during discovery, even tests that won't run due to filtering.

Current pipeline:
```
Source Gen → Full TestMetadata (20+ properties, delegates) → Filter → Build → Execute
                    ↑ EXPENSIVE                                  ↑ Most tests discarded
```

Proposed pipeline:
```
Source Gen → Lightweight Descriptor → Filter → Lazy Materialize → Build → Execute
                    ↑ CHEAP                          ↑ Only matching tests
```

## Current Architecture

### TestMetadata (Heavyweight)

```csharp
public abstract class TestMetadata
{
    // Identity (needed for filtering)
    public required string TestName { get; init; }
    public required Type TestClassType { get; init; }
    public required string TestMethodName { get; init; }

    // Location (needed for display)
    public required string FilePath { get; init; }
    public required int LineNumber { get; init; }

    // Expensive (delegates, reflection, allocations)
    public Func<Type[], object?[], object> InstanceFactory { get; init; }
    public Func<object, object?[], Task>? TestInvoker { get; init; }
    public required Func<Attribute[]> AttributeFactory { get; init; }
    public required IDataSourceAttribute[] DataSources { get; init; }
    public required IDataSourceAttribute[] ClassDataSources { get; init; }
    public required PropertyDataSource[] PropertyDataSources { get; init; }
    public required MethodMetadata MethodMetadata { get; init; }
    // ... 15+ more properties
}
```

**Problem**: All 20+ properties are populated during discovery, including:
- Delegates that capture closures (allocations)
- Arrays that are never used if test doesn't match filter
- Attribute factories that instantiate attributes

### ITestSource Interface

```csharp
public interface ITestSource
{
    IAsyncEnumerable<TestMetadata> GetTestsAsync(string testSessionId, CancellationToken ct);
}
```

**Problem**: Returns full `TestMetadata`, forcing eager materialization.

## Proposed Architecture

### Phase 1: Lightweight TestDescriptor

```csharp
/// <summary>
/// Minimal test identity for fast enumeration and filtering.
/// No allocations beyond the struct itself.
/// </summary>
public readonly struct TestDescriptor
{
    // Identity (for filtering) - all value types or interned strings
    public required string TestId { get; init; }
    public required string ClassName { get; init; }
    public required string MethodName { get; init; }
    public required string FullyQualifiedName { get; init; }

    // Location (for display)
    public required string FilePath { get; init; }
    public required int LineNumber { get; init; }

    // Filter hints (pre-computed during source gen)
    public required string[] Categories { get; init; }      // [Category("X")] values
    public required string[] Traits { get; init; }          // Trait key=value pairs
    public required bool HasDataSource { get; init; }       // Quick check for parameterized
    public required int RepeatCount { get; init; }          // Pre-extracted

    // Lazy materialization
    public required Func<string, TestMetadata> Materializer { get; init; }
}
```

**Key properties**:
- Struct (stack allocated)
- Only filter-relevant data
- Pre-computed filter hints (source gen does the work)
- Single `Materializer` delegate defers expensive work

### Phase 2: Two-Phase Interface

```csharp
/// <summary>
/// Fast test enumeration for filtering.
/// </summary>
public interface ITestDescriptorSource
{
    IEnumerable<TestDescriptor> EnumerateTests();
}

/// <summary>
/// Full test source (backward compatible).
/// </summary>
public interface ITestSource : ITestDescriptorSource
{
    IAsyncEnumerable<TestMetadata> GetTestsAsync(string testSessionId, CancellationToken ct);
}
```

### Phase 3: Source Generator Changes

Current generated code (simplified):
```csharp
public class MyTestClass_Tests : ITestSource
{
    public async IAsyncEnumerable<TestMetadata> GetTestsAsync(string sessionId, ...)
    {
        yield return new SourceGeneratedTestMetadata
        {
            TestName = "MyTest",
            TestClassType = typeof(MyTestClass),
            InstanceFactory = (types, args) => new MyTestClass(),
            TestInvoker = (instance, args) => ((MyTestClass)instance).MyTest(),
            AttributeFactory = () => new[] { new TestAttribute() },
            DataSources = new[] { ... },
            // ... 15+ more expensive properties
        };
    }
}
```

Proposed generated code:
```csharp
public class MyTestClass_Tests : ITestSource
{
    // Pre-computed at compile time (static readonly)
    private static readonly TestDescriptor _descriptor = new()
    {
        TestId = "MyTestClass.MyTest",
        ClassName = "MyTestClass",
        MethodName = "MyTest",
        FullyQualifiedName = "MyNamespace.MyTestClass.MyTest",
        FilePath = "MyTestClass.cs",
        LineNumber = 42,
        Categories = new[] { "Unit" },  // Pre-extracted
        Traits = Array.Empty<string>(),
        HasDataSource = false,
        RepeatCount = 0,
        Materializer = MaterializeTest
    };

    // Fast path: Just return pre-computed descriptor
    public IEnumerable<TestDescriptor> EnumerateTests()
    {
        yield return _descriptor;
    }

    // Slow path: Full materialization (only called for matching tests)
    private static TestMetadata MaterializeTest(string sessionId)
    {
        return new SourceGeneratedTestMetadata
        {
            // ... full properties
        };
    }

    // Backward compatible
    public async IAsyncEnumerable<TestMetadata> GetTestsAsync(string sessionId, ...)
    {
        yield return MaterializeTest(sessionId);
    }
}
```

### Phase 4: Pipeline Changes

```csharp
internal sealed class TestBuilderPipeline
{
    public async Task<IEnumerable<AbstractExecutableTest>> BuildTestsAsync(
        string testSessionId,
        ITestExecutionFilter? filter = null)
    {
        // Phase 1: Fast enumeration
        var descriptors = _dataCollector.EnumerateDescriptors();

        // Phase 2: Filter (no materialization yet)
        var matchingDescriptors = filter != null
            ? descriptors.Where(d => FilterMatches(d, filter))
            : descriptors;

        // Phase 3: Lazy materialization (only matching tests)
        var metadata = matchingDescriptors
            .Select(d => d.Materializer(testSessionId));

        // Phase 4: Build executable tests
        return await BuildTestsFromMetadataAsync(metadata);
    }

    private static bool FilterMatches(TestDescriptor d, ITestExecutionFilter filter)
    {
        // Fast filter check using pre-computed hints
        // No attribute instantiation, no reflection
        return filter.Matches(d.FullyQualifiedName, d.Categories, d.Traits);
    }
}
```

## Data Source Deferral (Advanced)

For parameterized tests, data sources can be deferred even further:

```csharp
public readonly struct TestDescriptor
{
    // For parameterized tests, descriptor represents the "template"
    // Each data row becomes a separate test during materialization
    public required bool HasDataSource { get; init; }
    public required int EstimatedDataRowCount { get; init; }  // Hint for capacity
}
```

During materialization:
```csharp
private static IEnumerable<TestMetadata> MaterializeTest(string sessionId)
{
    // Data source evaluation happens here, after filtering
    foreach (var dataRow in GetDataSource())
    {
        yield return new SourceGeneratedTestMetadata
        {
            // ... properties with dataRow values
        };
    }
}
```

## Implementation Plan

### Step 1: Add TestDescriptor (Non-Breaking)

1. Create `TUnit.Core/TestDescriptor.cs`
2. Create `TUnit.Core/Interfaces/SourceGenerator/ITestDescriptorSource.cs`
3. Make `ITestSource` extend `ITestDescriptorSource` with default implementation
4. Add unit tests

**Estimated scope**: 2 new files, 0 breaking changes

### Step 2: Update Source Generator

1. Modify `TestMetadataGenerator.cs` to generate:
   - Static `TestDescriptor` field with pre-computed values
   - `EnumerateTests()` method returning descriptors
   - `MaterializeTest()` factory method
2. Extract filter hints at compile time (categories, traits)
3. Update snapshot tests

**Estimated scope**: ~300 lines changed in source generator

### Step 3: Update Pipeline

1. Add `ITestDataCollector.EnumerateDescriptors()` method
2. Update `TestBuilderPipeline` to use two-phase approach
3. Implement fast filter matching against descriptors
4. Add fallback to full materialization for complex filters

**Estimated scope**: ~150 lines in pipeline

### Step 4: Optimize Reflection Mode

1. Update `ReflectionTestDataCollector` to support descriptors
2. Cache descriptor data per-type (not per-test)
3. Implement lazy materialization for reflection mode

**Estimated scope**: ~200 lines

### Step 5: Benchmarks and Validation

1. Run speed-comparison benchmarks
2. Target: Match or beat MSTest for single test execution
3. Validate AOT compatibility
4. Run full test suite

## Performance Expectations

| Scenario | Current | Expected | Improvement |
|----------|---------|----------|-------------|
| Single test (no filter) | 596ms | ~530ms | ~11% |
| Single test (with filter) | 596ms | ~480ms | ~20% |
| 1000 tests, run 10 | N/A | -30% time | Significant |
| Full suite | baseline | ~same | No regression |

Key wins:
1. **No delegate allocation** during enumeration (major GC improvement)
2. **No attribute instantiation** until materialization
3. **Pre-computed filter hints** avoid runtime reflection
4. **Only materialize tests that will run**

## Risks and Mitigations

### Risk: Breaking change for custom test sources

**Mitigation**: `ITestDescriptorSource` has default implementation that delegates to `GetTestsAsync()`. Existing sources continue to work, just without optimization.

### Risk: Source generator complexity

**Mitigation**: Implement incrementally. Phase 1 just adds descriptor alongside existing code. Only remove old code after validation.

### Risk: Filter hint extraction misses edge cases

**Mitigation**: Complex filters fall back to full materialization. Fast path is optimization, not requirement.

### Risk: Memory overhead of descriptor + metadata

**Mitigation**: Descriptor is struct (stack allocated). Materializer delegate is shared (static method). Net memory should decrease.

## Alternatives Considered

### Alternative 1: Lazy property initialization

Instead of separate descriptor, make `TestMetadata` properties lazy.

**Rejected**: Still allocates the object, still creates delegate captures. Doesn't solve GC pressure.

### Alternative 2: Compiled filter expressions

Generate filter-specific code at compile time.

**Rejected**: Too complex, doesn't handle runtime filters (VS Test Explorer).

### Alternative 3: Just optimize hot paths

Continue with micro-optimizations in existing architecture.

**Rejected**: Diminishing returns. Already applied sequential processing optimization. Fundamental architecture limits further gains.

## Success Criteria

1. Single test execution time <= MSTest (currently MSTest ~553ms, TUnit ~540ms after PR #4299)
2. No performance regression for full test suite
3. All existing tests pass
4. AOT/trimming compatibility maintained
5. Backward compatible with custom `ITestSource` implementations

## Next Steps

1. Review and approve this design
2. Create feature branch: `feature/lazy-test-materialization`
3. Implement Step 1 (TestDescriptor)
4. Iterate through remaining steps
5. Performance validation at each step
