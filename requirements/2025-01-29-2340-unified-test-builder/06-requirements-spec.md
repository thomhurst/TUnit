# Unified Test Builder Requirements Specification

## Problem Statement

TUnit currently has a complex `TestFactory` class (1139 lines) that handles multiple responsibilities including test building, data resolution, and generic type handling. The AOT and reflection paths are largely duplicated, leading to potential inconsistencies and maintenance challenges. The goal is to create a unified test builder that ensures feature parity between AOT and reflection modes while simplifying the architecture.

## Solution Overview

Implement a pipeline-based unified test builder with distinct stages:
1. **Data Collection** - Gather test metadata through either AOT or reflection
2. **Generic Resolution** - Pre-process generic types before expansion
3. **Data Expansion** - Expand data sources into test variations
4. **Test Creation** - Build executable tests from expanded data

## Functional Requirements

### 1. Pipeline Architecture
- **Requirement**: Implement a clear pipeline with distinct stages
- **Stages**:
  - Data Collection (AOT/Reflection variants)
  - Generic Type Resolution
  - Data Source Expansion
  - Test Instance Creation
- **Each stage**: Single responsibility, testable in isolation

### 2. Data Collection Layer
- **AOT Collector**:
  - Read from source-generated `TestMetadata`
  - Use pre-compiled invokers for test execution
  - Handle compile-time constant arguments directly
  - Create strongly-typed factories for method data sources
  - Instantiate and call AsyncDataSourceGenerator attributes
  
- **Reflection Collector**:
  - Scan assemblies for test attributes
  - Build `TestMetadata` at runtime
  - All data source attributes implement common interface
  - Interface provides `GenerateData()` returning `IEnumerable<Func<object?[]>>`

### 3. Data Source Handling
- **Factory Pattern**: Data sources return `IEnumerable<Func<object?[]>>` not materialized data
- **Test Isolation**: Each test invokes factory for fresh instances
- **Supported Types**:
  - Arguments attribute (static data)
  - MethodDataSource (dynamic from methods)
  - AsyncDataSourceGenerator (custom generators)
  - Property injection (filtered to first item)
- **Injection Targets**: Class constructors, method parameters, properties

### 4. Property Injection
- **First-Class Treatment**: Use same expansion logic as other data sources
- **Single Value**: When enumerable returned, use first item only
- **Timing**: Apply during test instance creation, not post-processing

### 5. Generic Type Handling
- **Pre-Processing Stage**: Resolve generics before data expansion
- **Support**: Both generic classes and generic methods
- **Type Inference**: From test data when possible
- **Error Handling**: Clear messages when types cannot be resolved

### 6. Test Expansion
- **Matrix Tests**: Support cartesian product of multiple data sources
- **Limits**: Configurable depth and combination limits
- **Unique IDs**: Include data source indices for uniqueness
- **Display Names**: Human-readable with parameter values

## Technical Requirements

### 1. AOT Compatibility
- **Source Generation Mode**: Zero reflection, no warnings
- **Pre-Compiled Invokers**: All test/instance creation via delegates
- **No Dynamic Code**: No runtime code generation
- **Trimming Safe**: All code paths analyzable at compile time

### 2. Reflection Mode Support
- **Languages**: F#, VB.NET, and other non-C# languages
- **Discovery**: Runtime attribute scanning
- **Compatibility**: Not required to be AOT-compatible
- **Feature Parity**: Same capabilities as AOT mode

### 3. Performance
- **Data Source Timeouts**: 30-second default, configurable
- **Lazy Evaluation**: Data sources evaluated only when needed
- **No Caching**: Fresh instances for test isolation
- **Parallel-Friendly**: Thread-safe test creation

### 4. Breaking Changes Allowed
- **Remove**: Old builder implementations
- **Restructure**: Test metadata if needed
- **Simplify**: Public API surface
- **Document**: Migration path for users

## Implementation Hints

### 1. Interface Design
```csharp
public interface ITestDataCollector
{
    Task<IEnumerable<TestMetadata>> CollectTests();
}

public interface IDataSourceExpander
{
    Task<IEnumerable<ExpandedTestData>> ExpandDataSources(TestMetadata metadata);
}

public interface ITestBuilder
{
    ExecutableTest BuildTest(TestMetadata metadata, ExpandedTestData data);
}
```

### 2. Pipeline Structure
```csharp
public class UnifiedTestBuilderPipeline
{
    private readonly ITestDataCollector _collector;
    private readonly IGenericTypeResolver _genericResolver;
    private readonly IDataSourceExpander _expander;
    private readonly ITestBuilder _builder;
    
    public async Task<IEnumerable<ExecutableTest>> BuildTests()
    {
        var metadata = await _collector.CollectTests();
        var resolved = await _genericResolver.ResolveGenerics(metadata);
        var expanded = await _expander.ExpandDataSources(resolved);
        return expanded.Select(data => _builder.BuildTest(data));
    }
}
```

### 3. Data Source Factory Pattern
```csharp
public interface IDataSource
{
    IEnumerable<Func<object?[]>> GenerateData(DataSourceContext context);
}
```

### 4. File Organization
- `TUnit.Engine/Building/UnifiedTestBuilder.cs` - Main pipeline
- `TUnit.Engine/Building/Collectors/` - AOT and Reflection collectors
- `TUnit.Engine/Building/Expanders/` - Data source expanders
- `TUnit.Engine/Building/Resolvers/` - Generic type resolver
- `TUnit.Core/DataSources/` - Updated data source interfaces

## Acceptance Criteria

1. **Feature Parity**: AOT and reflection modes produce identical test results
2. **Simplified Code**: Reduced complexity compared to current TestFactory
3. **Test Isolation**: No shared state between test instances
4. **Performance**: No regression in test discovery/execution time
5. **Extensibility**: Easy to add new data source types
6. **Testability**: Each pipeline stage independently testable
7. **Clear Errors**: Helpful messages for configuration issues
8. **Documentation**: Clear migration guide for breaking changes

## Assumptions

1. **Migration Path**: Users willing to update for cleaner architecture
2. **Testing Time**: Adequate time for testing all data source combinations
3. **Backward Compatibility**: Not required for internal structures
4. **Source Generator Updates**: Can modify generated code format
5. **API Surface**: Can redesign public interfaces as needed