# Refactoring Plan: Remove TestDataSource Abstraction

## Overview
Remove the `TestDataSource` and `AsyncTestDataSource` abstractions in favor of using `IDataSourceAttribute` directly. This will simplify the architecture and make it fully async-first.

## Current Architecture

### Problems
1. **Dual abstractions**: Both `IDataSourceAttribute` (async) and `TestDataSource` (sync) exist
2. **Sync/async bridging**: Complex bridging code to convert between sync and async patterns
3. **Redundant layers**: TestDataSource doesn't add significant value over IDataSourceAttribute
4. **Performance issues**: Synchronous operations can cause thread pool starvation

### Current Flow
```
IDataSourceAttribute 
  └─> TestDataSource (sync abstraction)
      └─> GetDataFactories() returns IEnumerable<Func<object?[]>>
          └─> Various implementations (Static, Delegate, etc.)

IDataSourceAttribute
  └─> AsyncTestDataSource (async abstraction)
      └─> GetDataFactoriesAsync() returns IAsyncEnumerable<Func<object?[]>>
          └─> Bridge methods to convert to sync
```

## Target Architecture

### Simplified Flow
```
IDataSourceAttribute
  └─> GetDataRowsAsync() returns IAsyncEnumerable<Func<Task<object?[]?>>>
      └─> Direct usage everywhere
```

## Refactoring Steps

### Phase 1: Create IDataSourceAttribute Implementations

#### 1.1 Create StaticDataSourceAttribute
```csharp
// TUnit.Core/Attributes/TestData/StaticDataSourceAttribute.cs
internal sealed class StaticDataSourceAttribute : Attribute, IDataSourceAttribute
{
    private readonly object?[][] _data;
    
    public StaticDataSourceAttribute(params object?[][] data)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
    }
    
    public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata metadata)
    {
        foreach (var row in _data)
        {
            var clonedRow = CloneArguments(row);
            yield return () => Task.FromResult<object?[]?>(clonedRow);
        }
    }
    
    private static object?[] CloneArguments(object?[] args)
    {
        var cloned = new object?[args.Length];
        Array.Copy(args, cloned, args.Length);
        return cloned;
    }
}
```

#### 1.2 Create DelegateDataSourceAttribute
```csharp
// TUnit.Core/Attributes/TestData/DelegateDataSourceAttribute.cs
internal sealed class DelegateDataSourceAttribute : Attribute, IDataSourceAttribute
{
    private readonly Func<DataGeneratorMetadata, IAsyncEnumerable<object?[]>> _factory;
    
    public DelegateDataSourceAttribute(Func<DataGeneratorMetadata, IAsyncEnumerable<object?[]>> factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }
    
    public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata metadata)
    {
        await foreach (var data in _factory(metadata))
        {
            var clonedData = CloneArguments(data);
            yield return () => Task.FromResult<object?[]?>(clonedData);
        }
    }
    
    private static object?[] CloneArguments(object?[] args) { ... }
}
```

#### 1.3 Create EmptyDataSourceAttribute
```csharp
// TUnit.Core/Attributes/TestData/EmptyDataSourceAttribute.cs
internal sealed class EmptyDataSourceAttribute : Attribute, IDataSourceAttribute
{
    public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata metadata)
    {
        yield return () => Task.FromResult<object?[]?>(Array.Empty<object?>());
    }
}
```

### Phase 2: Update TestMetadata

#### 2.1 Change TestMetadata Properties
```csharp
// TUnit.Core/TestMetadata.cs
public abstract class TestMetadata
{
    // Change from:
    // public TestDataSource[] DataSources { get; init; } = [];
    // public TestDataSource[] ClassDataSources { get; init; } = [];
    
    // To:
    public IDataSourceAttribute[] DataSources { get; init; } = [];
    public IDataSourceAttribute[] ClassDataSources { get; init; } = [];
    
    // PropertyDataSource needs updating too
}
```

#### 2.2 Update PropertyDataSource
```csharp
// TUnit.Core/TestDataSources.cs
public sealed class PropertyDataSource
{
    public required string PropertyName { get; init; }
    public required Type PropertyType { get; init; }
    // Change from: public required TestDataSource DataSource { get; init; }
    public required IDataSourceAttribute DataSource { get; init; }
}
```

### Phase 3: Update ReflectionTestMetadata

#### 3.1 Update Data Processing Methods
```csharp
// TUnit.Engine/Discovery/ReflectionTestMetadata.cs

// Remove sync processing methods:
// - ProcessMethodDataSource(TestDataSource dataSource)
// - ProcessClassDataSource(TestDataSource dataSource)

// Update async methods to work directly with IDataSourceAttribute:
private async Task<IEnumerable<MethodDataCombination>> ProcessMethodDataSourceAsync(IDataSourceAttribute dataSource)
{
    var combinations = new List<MethodDataCombination>();
    int loopIndex = 0;
    
    var metadata = new DataGeneratorMetadata
    {
        TestSessionId = SessionId,
        TestClassType = _testClass,
        TestMethodInfo = _testMethod,
        // ... other properties
    };
    
    await foreach (var factory in dataSource.GetDataRowsAsync(metadata))
    {
        var dataTask = factory();
        var data = await dataTask.ConfigureAwait(false);
        
        if (data != null)
        {
            var dataFactories = data.Select(value => new Func<Task<object?>>(
                () => Task.FromResult(value)
            )).ToArray();
            
            combinations.Add(new MethodDataCombination
            {
                DataFactories = dataFactories,
                DataSourceIndex = 0,
                LoopIndex = loopIndex++
            });
        }
    }
    
    return combinations;
}
```

### Phase 4: Update ReflectionTestDataCollector

#### 4.1 Update Extraction Methods
```csharp
// TUnit.Engine/Discovery/ReflectionTestDataCollector.cs

private static IDataSourceAttribute[] ExtractMethodDataSources(MethodInfo testMethod)
{
    var dataSources = new List<IDataSourceAttribute>();
    
    foreach (var attr in testMethod.GetCustomAttributes())
    {
        if (attr is IDataSourceAttribute dataSourceAttr)
        {
            dataSources.Add(dataSourceAttr);
        }
        else if (attr is ArgumentsAttribute argsAttr)
        {
            // Convert to StaticDataSourceAttribute
            dataSources.Add(new StaticDataSourceAttribute(argsAttr.Values));
        }
        // Handle other legacy attributes...
    }
    
    return dataSources.ToArray();
}
```

#### 4.2 Remove CreateDataSourceFromAttribute
Replace with direct attribute usage or minimal conversion logic.

### Phase 5: Update Source Generator

#### 5.1 Update CodeGenerationHelpers
```csharp
// TUnit.Core.SourceGenerator/CodeGenerationHelpers.cs

// Instead of generating TestDataSource instances, generate attribute instances:
private static void GenerateMethodDataSources(CodeWriter writer, IMethodSymbol method)
{
    writer.SetIndentLevel(2);
    using (writer.BeginArrayInitializer("new global::TUnit.Core.IDataSourceAttribute[]"))
    {
        foreach (var attr in method.GetAttributes())
        {
            if (IsDataSourceAttribute(attr))
            {
                writer.AppendLine(GenerateDataSourceAttribute(attr, method));
            }
        }
    }
}

private static string GenerateDataSourceAttribute(AttributeData attr, IMethodSymbol method)
{
    if (attr.AttributeClass?.Name == "ArgumentsAttribute")
    {
        // Generate StaticDataSourceAttribute
        return $"new global::TUnit.Core.StaticDataSourceAttribute({GenerateArgumentsArray(attr)})";
    }
    // ... handle other attributes
}
```

### Phase 6: Remove Old Classes

#### 6.1 Delete Files
- `TUnit.Core/TestDataSources.cs` (except PropertyDataSource which gets updated)
- `TUnit.Core/AsyncTestDataSources.cs`

#### 6.2 Remove References
- Update all imports
- Remove any TestDataSource-specific logic

### Phase 7: Update Tests and Documentation

#### 7.1 Update Unit Tests
- Rewrite tests that use TestDataSource directly
- Ensure async patterns are properly tested

#### 7.2 Update Documentation
- Update API documentation
- Update examples to show direct IDataSourceAttribute usage

## Migration Strategy

### Backward Compatibility Considerations
1. Keep ArgumentsAttribute, MethodDataSourceAttribute, etc. as-is
2. Internal conversion to IDataSourceAttribute happens transparently
3. No breaking changes to public API

### Performance Improvements
1. Eliminate sync-over-async patterns
2. Reduce object allocations
3. Simplify execution path

### Testing Strategy
1. Run all existing tests to ensure no regressions
2. Add specific tests for async data source processing
3. Performance benchmarks before/after

## Benefits

1. **Simpler Architecture**: One clear async-first pattern
2. **Better Performance**: No sync/async bridging overhead
3. **Cleaner Code**: Less abstraction layers
4. **Future-Proof**: Async-first design aligns with modern .NET
5. **Easier Maintenance**: Fewer concepts to understand

## Risks and Mitigations

1. **Risk**: Large refactoring could introduce bugs
   - **Mitigation**: Comprehensive test coverage, phased approach

2. **Risk**: Source generator complexity
   - **Mitigation**: Generate simple attribute instances, minimize logic

3. **Risk**: Performance regression in some scenarios
   - **Mitigation**: Benchmark critical paths before/after

## Implementation Order

1. Create new attribute implementations (Phase 1)
2. Update discovery/reflection code (Phases 3-4)
3. Update source generator (Phase 5)
4. Update TestMetadata last (Phase 2)
5. Remove old code (Phase 6)
6. Update tests/docs (Phase 7)

This order allows for incremental changes and testing at each phase.