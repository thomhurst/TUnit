# Architectural Unification Analysis: Source Generation vs Reflection Modes

## Executive Summary

After analyzing the TUnit codebase, I've identified significant opportunities for architectural unification between the source generation and reflection modes. While both modes share the same core abstractions, they diverge unnecessarily in several areas after metadata collection, leading to code duplication and potential inconsistencies.

## Key Areas of Divergence

### 1. ExecutableTest Creation Patterns

**Current State:**
- **Source Generation**: Uses strongly-typed `ExecutableTest<T>` with compile-time generated factories
- **Reflection**: Uses `DynamicExecutableTest` with runtime delegates

**Issues:**
- Different handling of CancellationToken injection
- Duplicate logic for property injection
- Inconsistent error handling patterns

**Recommendation:**
Create a unified `ExecutableTestFactory` that:
```csharp
public interface IExecutableTestFactory
{
    ExecutableTest Create(ExecutableTestCreationContext context, TestMetadata metadata);
}

// Single implementation that handles both modes
public class UnifiedExecutableTestFactory : IExecutableTestFactory
{
    public ExecutableTest Create(ExecutableTestCreationContext context, TestMetadata metadata)
    {
        // Use metadata.CreateExecutableTestFactory delegate - works for both modes
        return metadata.CreateExecutableTestFactory(context, metadata);
    }
}
```

### 2. Data Combination Handling

**Current State:**
- **Source Generation**: Complex compile-time expansion in `DataCombinationGeneratorEmitter`
- **Reflection**: Runtime expansion in `ReflectionTestMetadata.GenerateDataCombinations()`

**Issues:**
- Completely different code paths for the same logic
- Source generation has better error handling (wraps exceptions in TestDataCombination)
- Reflection mode doesn't support instance method data sources properly

**Recommendation:**
Extract shared data combination logic into reusable components:
```csharp
public static class DataCombinationBuilder
{
    public static async IAsyncEnumerable<TestDataCombination> BuildCombinations(
        TestDataSource[] methodSources,
        TestDataSource[] classSources,
        PropertyDataSource[] propertySources,
        int repeatCount)
    {
        // Shared logic for cartesian product generation
        // Both modes can use this with their specific data source implementations
    }
}
```

### 3. Event Receiver Processing

**Current State:**
- Both modes use `EventReceiverOrchestrator`, but initialization differs
- Source generation pre-registers receivers, reflection discovers them at runtime

**Issues:**
- Inconsistent discovery timing
- Reflection mode has to scan assemblies repeatedly

**Recommendation:**
Create a unified discovery pipeline:
```csharp
public interface IEventReceiverDiscovery
{
    Task<IEnumerable<IEventReceiver>> DiscoverReceiversAsync(TestContext context);
}

// Single orchestrator works with discovered receivers regardless of source
public class UnifiedEventReceiverOrchestrator
{
    private readonly IEventReceiverDiscovery _discovery;
    
    public async Task InitializeAsync(TestContext context)
    {
        var receivers = await _discovery.DiscoverReceiversAsync(context);
        _registry.RegisterReceivers(receivers);
    }
}
```

### 4. Hook Collection and Processing

**Current State:**
- **Source Generation**: Hooks are pre-registered in `Sources.TestHookSources`
- **Reflection**: Hooks are discovered by scanning type hierarchies

**Issues:**
- Different caching strategies
- Reflection mode misses some assembly-level hooks
- Duplicate filtering logic

**Recommendation:**
Unify hook discovery:
```csharp
public interface IHookDiscovery
{
    Task<TestHooks> DiscoverHooksAsync(Type testClassType, string testSessionId);
}

// Both modes implement this interface
// Source generation returns pre-discovered hooks
// Reflection performs runtime discovery
// HookCollectionService works with either implementation
```

### 5. Test Instance Creation

**Current State:**
- Different property injection mechanisms
- Different initialization order for nested data sources

**Issues:**
- Source generation handles nested data source properties at compile time
- Reflection tries to handle them at runtime with incomplete support

**Recommendation:**
Standardize instance creation:
```csharp
public interface ITestInstanceFactory
{
    Task<object> CreateInstanceAsync(
        TestMetadata metadata,
        object?[] constructorArgs,
        Dictionary<string, object?> propertyValues);
}

// Single implementation that:
// 1. Creates instance using metadata.InstanceFactory
// 2. Applies property values consistently
// 3. Initializes data source properties using registered helpers
// 4. Calls ObjectInitializer.InitializeAsync
```

## Specific Unification Opportunities

### 1. TestMetadata Base Class Enhancement

The `TestMetadata` base class already provides good abstraction, but could be enhanced:

```csharp
public abstract class TestMetadata
{
    // Existing properties...
    
    // Add unified hooks for customization
    public virtual Task OnBeforeInstanceCreation(TestContext context) => Task.CompletedTask;
    public virtual Task OnAfterInstanceCreation(object instance, TestContext context) => Task.CompletedTask;
    
    // Standardize data combination generation
    public abstract IAsyncEnumerable<TestDataCombination> GenerateDataCombinationsAsync(
        DataCombinationContext context);
}
```

### 2. Shared Data Source Processing

Create shared utilities for data source processing:

```csharp
public static class DataSourceProcessor
{
    // Used by both modes
    public static Func<Task<object?>>[] ProcessDataSource(
        object? dataSourceResult,
        int expectedParameterCount)
    {
        // Handle IEnumerable, arrays, tuples, Func<T>, etc.
        // Single implementation for both modes
    }
    
    public static async Task<object?> ResolveDataSourceValue(object? value)
    {
        // Handle Func<T> invocation, tuple unpacking, etc.
    }
}
```

### 3. Unified Test Execution Pipeline

The execution pipeline (`UnifiedTestExecutor`) already works well with both modes, but could be enhanced:

```csharp
public interface ITestExecutionPipeline
{
    // Hooks for mode-specific behavior
    Task OnBeforeTestExecution(ExecutableTest test);
    Task OnAfterTestExecution(ExecutableTest test, TestResult result);
}

// Source generation and reflection can provide custom implementations
// while sharing the core execution logic
```

### 4. Property Injection Unification

Currently scattered across multiple places:

```csharp
public interface IPropertyInjector
{
    Task InjectPropertiesAsync(
        object instance,
        Dictionary<string, object?> propertyValues,
        TestMetadata metadata);
}

// Single implementation that:
// 1. Uses PropertyInjectionData from metadata when available (source gen)
// 2. Falls back to reflection for dynamic scenarios
// 3. Handles data source property initialization consistently
```

## Performance Implications

### Positive Impacts:
1. **Reduced Code Paths**: Single implementation means better CPU cache utilization
2. **Shared Caching**: Unified caching strategies for hooks, event receivers
3. **Optimized Allocations**: Reusable components reduce object allocations

### Potential Concerns:
1. **Abstraction Overhead**: Additional interfaces may add virtual dispatch overhead
2. **Mode Detection**: Runtime checks for mode-specific behavior

### Mitigation Strategies:
1. Use generic type parameters where possible to enable JIT optimizations
2. Implement mode-specific fast paths for critical sections
3. Use source generation to create mode-specific implementations of interfaces

## Implementation Priority

1. **High Priority** (Immediate benefits, low risk):
   - Unify data source processing utilities
   - Standardize property injection
   - Consolidate error handling patterns

2. **Medium Priority** (Significant benefits, moderate effort):
   - Create unified ExecutableTest factory
   - Standardize event receiver discovery
   - Unify hook collection pipeline

3. **Low Priority** (Nice to have, higher risk):
   - Full data combination generation unification
   - Complete abstraction of mode-specific behaviors

## Code Duplication to Eliminate

### 1. DataSourceHelpers (multiple implementations):
- `TUnit.Core.Helpers.DataSourceHelpers`
- Inline implementations in reflection mode
- Generated helpers in source generation

**Unify into:** Single `DataSourceHelpers` class used by both modes

### 2. Property Initialization:
- `EmitInstanceDataSourcePropertyInitialization` in source generation
- `InitializeNestedPropertiesAsync` in reflection
- Runtime property injection in instance creation

**Unify into:** Single property initialization pipeline

### 3. Hook Discovery:
- Source generation: compile-time discovery
- Reflection: runtime type scanning
- Different caching mechanisms

**Unify into:** Common hook metadata format with mode-specific discovery

### 4. Test Name Generation:
- Different formatting in each mode
- Inconsistent parameter display

**Unify into:** Shared test naming utility

## Conclusion

The architectural unification opportunities in TUnit are significant and achievable. The codebase already has good abstractions in place (TestMetadata, ExecutableTest, etc.), but the implementations diverge unnecessarily after the metadata collection phase.

By implementing the recommended unifications:
1. Code maintenance becomes easier with less duplication
2. Bugs fixed in one mode automatically benefit the other
3. New features can be implemented once for both modes
4. Performance optimizations benefit the entire system

The key is to maintain the benefits of each mode (compile-time safety for source generation, runtime flexibility for reflection) while sharing as much implementation as possible.