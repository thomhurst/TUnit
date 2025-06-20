# TUnit Clean Architecture Migration Guide

## Overview

TUnit has been refactored to use a clean architecture that separates compile-time discovery from runtime execution. This guide helps developers understand and migrate to the new architecture.

## Architecture Changes

### Before (Legacy)
- Source generators emitted complex execution code
- Test logic was baked into generated strings
- Difficult to debug and maintain
- Limited optimization opportunities

### After (Clean Architecture)
- Source generators emit only `TestMetadata` data structures
- `TestBuilder` handles all runtime logic
- Easy to debug and extend
- Optimized with expression compilation and caching

## Key Components

### 1. TestMetadataGenerator (Source Generator)
```csharp
// Only emits data, no logic
testMetadata.Add(new TestMetadata
{
    TestIdTemplate = "MyTest_{TestIndex}",
    TestClassType = typeof(MyTestClass),
    TestMethod = methodInfo,
    ClassDataSources = dataProviders,
    // ... other metadata
});
```

### 2. TestBuilder (Runtime Engine)
```csharp
// Handles all complex logic
public async Task<IEnumerable<TestDefinition>> BuildTestsAsync(TestMetadata metadata)
{
    // Enumerate data sources
    // Unwrap tuples
    // Create test instances
    // Inject properties
    // Build executable test definitions
}
```

## Migration Guide for Extension Authors

### If You Have Custom Source Generators

**Before:**
```csharp
// Generating complex execution code
writer.WriteLine($@"
    var instance = new {className}({args});
    foreach (var data in GetData())
    {{
        var unwrapped = UnwrapTuple(data);
        instance.{methodName}(unwrapped);
    }}
");
```

**After:**
```csharp
// Just emit metadata
var metadata = new TestMetadata
{
    TestClassType = classType,
    TestMethod = methodInfo,
    MethodDataSources = new[] { new MethodDataSourceProvider(type, methodName) }
};
```

### If You Have Custom Data Sources

No changes needed! The `IDataSourceProvider` interface remains the same:

```csharp
public class MyDataSourceProvider : IDataSourceProvider
{
    public async Task<IEnumerable<object?[]>> GetDataAsync(CancellationToken cancellationToken)
    {
        // Your data source logic
    }
}
```

### If You Have Custom Test Executors

The test execution pipeline remains unchanged. Your custom executors will receive `TestDefinition` instances as before.

## Benefits of Migration

### 1. Performance
- Expression compilation for method invocation
- Caching of reflection operations
- Parallel data source processing

### 2. Maintainability
- Source generator is simpler
- Business logic in testable runtime code
- Clear separation of concerns

### 3. Debuggability
- Step through actual TestBuilder code
- See real stack traces
- Inspect runtime state

### 4. Extensibility
- Easy to add new features
- Modify behavior without changing source generator
- Plugin architecture for custom builders

## Configuration

The clean architecture is now the default. No configuration needed!

### TestBuilder Features

The TestBuilder includes built-in optimizations:
- Expression compilation for method invocation
- Caching of reflection operations
- Parallel data source processing
- Automatic tuple unwrapping
- Property injection support

Enable diagnostics for troubleshooting:
```csharp
TUnitConfiguration.EnableDiagnostics = true;
```

## Troubleshooting

### Tests Not Discovered

1. Ensure `TestMetadataGenerator` is enabled (it's the only generator now)
2. Check that your test methods have `[Test]` attribute
3. Verify test class is public and not abstract

### Performance Issues

1. Use `Optimized` mode (default)
2. Enable caching for expensive data sources with `Shared = true`
3. Check diagnostic output with `WithDiagnostics` mode

### Custom Logic Not Working

1. Implement logic in a custom `ITestBuilder` if needed
2. Use TestBuilder's extension points
3. File an issue for missing functionality

## Examples

See `/Examples/CleanArchitectureExample.cs` for comprehensive examples of:
- Simple tests
- Data-driven tests
- Property injection
- Class construction
- Complex scenarios

## Summary

The clean architecture makes TUnit:
- **Faster** - Optimized runtime execution
- **Simpler** - Less generated code
- **More Maintainable** - Clear separation of concerns
- **More Extensible** - Easy to add features

No action required for most users - it just works better!