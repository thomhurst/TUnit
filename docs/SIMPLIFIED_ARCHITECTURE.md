# TUnit Simplified Architecture

## Overview

The TUnit architecture has been simplified to reduce complexity by ~50% while maintaining all functionality. This document describes the new streamlined architecture.

## Core Components

### 1. TestMetadata (Compile-Time)

The `TestMetadata` class is the unified compile-time representation of a test, supporting both AOT and reflection scenarios:

```csharp
public sealed class TestMetadata
{
    // Test identification
    public required string TestId { get; init; }
    public required string TestName { get; init; }
    public required Type TestClassType { get; init; }
    public required string TestMethodName { get; init; }
    
    // Test configuration
    public string[] Categories { get; init; } = [];
    public bool IsSkipped { get; init; }
    public string? SkipReason { get; init; }
    public int? TimeoutMs { get; init; }
    public int RetryCount { get; init; }
    public bool CanRunInParallel { get; init; } = true;
    public string[] DependsOn { get; init; } = [];
    
    // Data sources
    public TestDataSource[] DataSources { get; init; } = [];
    public PropertyDataSource[] PropertyDataSources { get; init; } = [];
    
    // AOT-friendly execution
    public Func<object>? InstanceFactory { get; init; }
    public Func<object, object?[], Task>? TestInvoker { get; init; }
    
    // Reflection fallback
    public MethodInfo? MethodInfo { get; init; }
    
    // Source location
    public string? FilePath { get; init; }
    public int? LineNumber { get; init; }
}
```

### 2. ExecutableTest (Runtime)

The `ExecutableTest` class represents a test ready for execution:

```csharp
public sealed class ExecutableTest
{
    // Identification
    public required string TestId { get; init; }
    public required string DisplayName { get; init; }
    public required TestMetadata Metadata { get; init; }
    
    // Execution details
    public required object?[] Arguments { get; init; }
    public required Func<Task<object>> CreateInstance { get; init; }
    public required Func<object, Task> InvokeTest { get; init; }
    public Dictionary<string, object?> PropertyValues { get; init; }
    public required TestLifecycleHooks Hooks { get; init; }
    
    // Runtime state
    public TestContext? Context { get; set; }
    public ExecutableTest[] Dependencies { get; set; } = [];
    public TestState State { get; set; }
    public TestResult? Result { get; set; }
    public DateTimeOffset? StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
}
```

### 3. TestFactory

The `TestFactory` replaces multiple builder classes with a single, clean factory:

```csharp
public sealed class TestFactory
{
    public async Task<IEnumerable<ExecutableTest>> CreateTests(TestMetadata metadata)
    {
        // Expands data-driven tests
        // Creates ExecutableTest instances
        // Handles property injection
        // Sets up lifecycle hooks
    }
}
```

### 4. UnifiedTestExecutor

The `UnifiedTestExecutor` provides clear execution paths:

```csharp
public sealed class UnifiedTestExecutor : ITestExecutor
{
    public async Task ExecuteTests(
        IEnumerable<ExecutableTest> tests,
        ITestExecutionFilter? filter,
        IMessageBus messageBus,
        CancellationToken cancellationToken)
    {
        // Groups tests by parallelization capability
        // Executes parallel tests with controlled concurrency
        // Executes serial tests respecting dependencies
        // Reports results via message bus
    }
}
```

### 5. Source Generation

The `UnifiedTestMetadataGenerator` creates compile-time test registrations:

```csharp
[Generator]
public class UnifiedTestMetadataGenerator : IIncrementalGenerator
{
    // Discovers [Test] methods at compile time
    // Generates TestMetadata instances
    // Creates AOT-friendly invokers
    // Registers with TestMetadataRegistry
}
```

## Architecture Flow

### Test Discovery Flow

```
Source Generator (Compile Time)
    ↓
TestMetadata Creation
    ↓
TestMetadataRegistry.RegisterSource()
    ↓
TestDiscoveryService.DiscoverTests()
    ↓
TestFactory.CreateTests()
    ↓
ExecutableTest instances
```

### Test Execution Flow

```
ExecutableTest
    ↓
UnifiedTestExecutor.ExecuteTests()
    ↓
Parallel/Serial Execution
    ↓
SingleTestExecutor.ExecuteTestAsync()
    ↓
Test Instance Creation
    ↓
Hook Execution
    ↓
Test Method Invocation
    ↓
Result Reporting
```

## Key Improvements

### 1. Reduced Layers
- Old: TestNodeBuilder → TestBuilder → TestConstructor → TestsConstructor
- New: TestFactory (single responsibility)

### 2. Unified Data Model
- Old: Multiple test representations (DiscoveredTest, Test, TestContext, etc.)
- New: TestMetadata (compile-time) + ExecutableTest (runtime)

### 3. Clear Execution Model
- Old: Complex orchestrators and coordinators
- New: UnifiedTestExecutor with straightforward parallel/serial paths

### 4. AOT-Friendly Design
- Compile-time metadata generation
- Minimal reflection with proper annotations
- Source-generated invokers for test execution

### 5. Simplified Hooks
- Old: Multiple hook orchestrators and executors
- New: Direct hook arrays in ExecutableTest

## Migration Guide

### For Test Authors
No changes required - all existing test attributes and patterns continue to work.

### For Extension Authors
1. Implement `ITestMetadataSource` to provide custom test discovery
2. Use `TestFactory` to create executable tests from metadata
3. Hook into execution via `ISingleTestExecutor` implementation

### For Framework Contributors
1. All core logic is in the simplified components listed above
2. Source generation logic is in `UnifiedTestMetadataGenerator`
3. Platform integration is through `SimplifiedTUnitTestFramework`

## Performance Characteristics

- **Compile-time discovery**: Zero runtime reflection cost for standard tests
- **Parallel execution**: Controlled concurrency with semaphore throttling
- **Memory efficient**: Single test representation throughout lifecycle
- **AOT compatible**: Full native AOT support with trimming