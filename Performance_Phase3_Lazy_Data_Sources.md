# Phase 3: Lazy Data Source Evaluation

## Overview
Implement lazy evaluation for test data sources to avoid expanding all test combinations during discovery. Data should only be materialized when tests are about to execute.

## Goals
1. Reduce memory usage for data-driven tests
2. Speed up discovery for tests with large data sets
3. Maintain compatibility with existing data source APIs
4. Support both sync and async data sources

## Current Problem
- All test data combinations expanded during discovery
- Creates N test instances for N data rows immediately
- Memory bloat for large data sets (e.g., 1000 rows = 1000 test objects)
- Discovery blocked while data sources execute

## Design Principles
- **SRP**: Data sources responsible only for providing data when needed
- **KISS**: Simple wrapper pattern for existing data sources
- **DRY**: Reuse existing data source implementations
- **AOT Compatible**: No runtime type generation or dynamic dispatch

## Implementation Plan

### Step 1: Create Lazy Data Source Wrapper
Create `TUnit.Engine/Data/LazyTestDataSource.cs`:

```csharp
namespace TUnit.Engine.Data;

/// <summary>
/// Wraps a test data source to provide lazy evaluation
/// </summary>
internal sealed class LazyTestDataSource : TestDataSource
{
    private readonly Func<CancellationToken, Task<IEnumerable<object?[]>>> _dataFactory;
    private readonly string _sourceDescription;
    private IEnumerable<object?[]>? _cachedData;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    
    public LazyTestDataSource(
        Func<CancellationToken, Task<IEnumerable<object?[]>>> dataFactory,
        string sourceDescription)
    {
        _dataFactory = dataFactory;
        _sourceDescription = sourceDescription;
    }
    
    public override async Task<IEnumerable<object?[]>> GetDataAsync(CancellationToken cancellationToken)
    {
        if (_cachedData != null)
        {
            return _cachedData;
        }
        
        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (_cachedData != null)
            {
                return _cachedData;
            }
            
            _cachedData = await _dataFactory(cancellationToken);
            return _cachedData;
        }
        finally
        {
            _loadLock.Release();
        }
    }
    
    public override string ToString() => _sourceDescription;
}

/// <summary>
/// Lazy wrapper for async enumerable data sources
/// </summary>
internal sealed class LazyAsyncEnumerableDataSource : TestDataSource
{
    private readonly Func<CancellationToken, IAsyncEnumerable<object?[]>> _dataFactory;
    private readonly string _sourceDescription;
    
    public LazyAsyncEnumerableDataSource(
        Func<CancellationToken, IAsyncEnumerable<object?[]>> dataFactory,
        string sourceDescription)
    {
        _dataFactory = dataFactory;
        _sourceDescription = sourceDescription;
    }
    
    public override async Task<IEnumerable<object?[]>> GetDataAsync(CancellationToken cancellationToken)
    {
        var results = new List<object?[]>();
        await foreach (var item in _dataFactory(cancellationToken).WithCancellation(cancellationToken))
        {
            results.Add(item);
        }
        return results;
    }
    
    public override string ToString() => _sourceDescription;
}
```

### Step 2: Create Test Data Expander Service
Create `TUnit.Engine/Services/TestDataExpander.cs`:

```csharp
namespace TUnit.Engine.Services;

/// <summary>
/// Expands test data on-demand rather than during discovery
/// </summary>
internal sealed class TestDataExpander
{
    private readonly ILogger _logger;
    
    public TestDataExpander(ILogger logger)
    {
        _logger = logger;
    }
    
    /// <summary>
    /// Creates a single test with lazy data evaluation
    /// </summary>
    public ExecutableTest CreateLazyDataTest(TestMetadata metadata, string testSessionId)
    {
        // Create test with placeholder for data
        var test = new ExecutableTest
        {
            TestId = GenerateTestId(metadata),
            DisplayName = metadata.TestName + " [Data]",
            Metadata = metadata,
            TestSessionId = testSessionId,
            LazyDataSource = CreateLazyDataSource(metadata)
        };
        
        return test;
    }
    
    /// <summary>
    /// Expands a lazy test into concrete test instances
    /// </summary>
    public async IAsyncEnumerable<ExecutableTest> ExpandTestDataAsync(
        ExecutableTest lazyTest,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (lazyTest.LazyDataSource == null)
        {
            yield return lazyTest;
            yield break;
        }
        
        var dataRows = await lazyTest.LazyDataSource.GetDataAsync(cancellationToken);
        var index = 0;
        
        foreach (var dataRow in dataRows)
        {
            var expandedTest = CloneTestWithData(lazyTest, dataRow, index++);
            yield return expandedTest;
        }
    }
    
    private LazyTestDataSource? CreateLazyDataSource(TestMetadata metadata)
    {
        // Combine all data sources into a single lazy source
        var dataSources = new List<TestDataSource>();
        
        if (metadata.DataSources?.Length > 0)
        {
            dataSources.AddRange(metadata.DataSources);
        }
        
        if (metadata.ClassDataSources?.Length > 0)
        {
            dataSources.AddRange(metadata.ClassDataSources.Select(cds => cds.DataSource));
        }
        
        if (metadata.PropertyDataSources?.Length > 0)
        {
            dataSources.AddRange(metadata.PropertyDataSources.Select(pds => pds.DataSource));
        }
        
        if (dataSources.Count == 0)
        {
            return null;
        }
        
        // Create lazy wrapper that combines all sources
        return new LazyTestDataSource(
            async cancellationToken =>
            {
                var allData = new List<object?[]>();
                foreach (var source in dataSources)
                {
                    var data = await source.GetDataAsync(cancellationToken);
                    allData.AddRange(data);
                }
                return GenerateDataCombinations(allData);
            },
            $"Combined data from {dataSources.Count} sources");
    }
    
    private ExecutableTest CloneTestWithData(ExecutableTest lazyTest, object?[] data, int index)
    {
        return new ExecutableTest
        {
            TestId = $"{lazyTest.TestId}_{index}",
            DisplayName = FormatTestName(lazyTest.DisplayName, data, index),
            Metadata = lazyTest.Metadata,
            TestSessionId = lazyTest.TestSessionId,
            TestData = data,
            LazyDataSource = null // Expanded test has concrete data
        };
    }
}
```

### Step 3: Update Test Builder
Modify test building to create lazy tests:

```csharp
// In TestBuilder.cs
public ExecutableTest? BuildExecutableTest(TestMetadata metadata, string testSessionId)
{
    if (HasDataSources(metadata))
    {
        // Create lazy test instead of expanding immediately
        return _dataExpander.CreateLazyDataTest(metadata, testSessionId);
    }
    
    // Regular test without data sources
    return BuildConcreteTest(metadata, testSessionId, testData: null);
}

private bool HasDataSources(TestMetadata metadata)
{
    return (metadata.DataSources?.Length > 0) ||
           (metadata.ClassDataSources?.Length > 0) ||
           (metadata.PropertyDataSources?.Length > 0);
}
```

### Step 4: Update Test Executor
Expand lazy tests just before execution:

```csharp
// In UnifiedTestExecutor.cs
public async Task ExecuteTests(
    IEnumerable<ExecutableTest> tests,
    ITestExecutionFilter? filter,
    IMessageBus messageBus,
    CancellationToken cancellationToken)
{
    var expandedTests = new List<ExecutableTest>();
    
    foreach (var test in tests)
    {
        if (test.LazyDataSource != null)
        {
            // Expand lazy test into concrete instances
            await foreach (var expanded in _dataExpander.ExpandTestDataAsync(test, cancellationToken))
            {
                if (filter == null || await filter.ShouldRunTest(expanded))
                {
                    expandedTests.Add(expanded);
                }
            }
        }
        else
        {
            if (filter == null || await filter.ShouldRunTest(test))
            {
                expandedTests.Add(test);
            }
        }
    }
    
    // Execute expanded tests
    await ExecuteTestsCore(expandedTests, executorAdapter, cancellationToken);
}
```

### Step 5: Update Data Source Creation (Reflection Mode)
Make reflection data sources lazy:

```csharp
// In ReflectionTestDataCollector.cs
private static TestDataSource? CreateMethodDataSource(MethodDataSourceAttribute attr, Type defaultClass)
{
    var targetClass = attr.ClassProvidingDataSource ?? defaultClass;
    var method = targetClass.GetMethod(attr.MethodNameProvidingDataSource, /* binding flags */);
    
    if (method == null) return null;
    
    // Create lazy wrapper instead of invoking immediately
    return new LazyTestDataSource(
        async cancellationToken =>
        {
            var instance = method.IsStatic ? null : Activator.CreateInstance(targetClass);
            var result = method.Invoke(instance, attr.Arguments);
            
            if (result is Task<IEnumerable<object?[]>> taskResult)
            {
                return await taskResult;
            }
            else if (result is IEnumerable<object?[]> enumerable)
            {
                return enumerable;
            }
            // Handle other return types...
            
            return Array.Empty<object?[]>();
        },
        $"MethodDataSource: {targetClass.Name}.{method.Name}");
}
```

## Migration Strategy
1. Implement lazy wrappers without changing public APIs
2. Update builders to create lazy tests
3. Expand tests at execution time
4. Monitor memory usage improvements

## Testing Strategy
1. Verify data sources execute only when needed
2. Test with large data sets (10,000+ rows)
3. Ensure data source exceptions handled correctly
4. Validate filtering works with lazy expansion

## Success Metrics
- 90%+ reduction in memory for large data-driven tests
- Near-zero discovery time for data-driven tests
- Data source execution deferred until test runs

## Risks and Mitigations
- **Risk**: Data source exceptions during execution
  - **Mitigation**: Catch and report as test failures
- **Risk**: Breaking change for custom data sources
  - **Mitigation**: Support both eager and lazy modes

## AOT Compatibility Notes
- No dynamic type creation
- Delegates captured at compile time
- Task and IAsyncEnumerable are AOT-safe
- All generic types resolved at build

## Next Steps
After implementation:
1. Benchmark memory usage with large data sets
2. Profile discovery time improvements
3. Move to Phase 4: Object Pooling