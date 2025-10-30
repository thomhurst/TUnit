# TestContext Interface Organization Migration Guide

## Overview

TUnit has reorganized the `TestContext` API to provide a cleaner, more discoverable interface structure. Properties and methods are now organized into logical, focused interfaces that group related functionality together.

This migration guide helps you update code that directly accesses `TestContext` properties to use the new interface-based API.

## What Changed

### New Interface Organization

`TestContext` now exposes its API through focused interface properties:

```csharp
public partial class TestContext :
    ITestExecution,
    ITestParallelization,
    ITestOutput,
    ITestMetadata,
    ITestDependencies,
    ITestStateBag,
    ITestEvents
{
    // Organized API access through interface properties
    public ITestExecution Execution => this;
    public ITestParallelization Parallelism => this;
    public ITestOutput Output => this;
    public ITestMetadata Metadata => this;
    public ITestDependencies Dependencies => this;
    public ITestStateBag StateBag => this;
    public ITestEvents Events => this;
    public IServiceProvider Services => ServiceProvider;
}
```

### Property Reorganization

Several properties have been moved from the main `TestContext` class into their appropriate interfaces:

#### `ITestExecution` - Execution State and Lifecycle

**New members:**
- `CustomHookExecutor` - Custom hook executor for test-level hooks
- `ReportResult` - Whether test results should be reported
- `AddLinkedCancellationToken()` - Link external cancellation tokens

**Existing members:**
- `Phase` - Current test phase (Discovery, Execution, Cleanup, etc.)
- `Result` - Test result after execution completes
- `CancellationToken` - Cancellation token for this test
- `TestStart` - Test execution start timestamp
- `TestEnd` - Test execution end timestamp
- `CurrentRetryAttempt` - Current retry attempt number
- `SkipReason` - Reason why test was skipped
- `RetryFunc` - Retry function for failed tests
- `OverrideResult()` - Override test result methods

#### `ITestMetadata` - Test Identity and Metadata

**New member:**
- `DisplayNameFormatter` - Custom display name formatter type

**Existing members:**
- `Id` - Unique identifier for this test instance
- `TestDetails` - Detailed metadata about the test
- `TestName` - Base name of the test method
- `DisplayName` - Display name for the test (get/set)

#### `ITestEvents` - Test Event Integration

**New interface** exposing:
- `Events` - Event manager for test lifecycle integration

## Migration Steps

### Direct Property Access

If you were directly accessing properties on `TestContext`, they now need to be accessed through the appropriate interface property.

#### Execution-Related Properties

**Before:**
```csharp
// ❌ Old - Direct access
var customExecutor = TestContext.Current.CustomHookExecutor;
TestContext.Current.ReportResult = false;
TestContext.Current.AddLinkedCancellationToken(externalToken);
```

**After:**
```csharp
// ✅ New - Through Execution interface
var customExecutor = TestContext.Current.Execution.CustomHookExecutor;
TestContext.Current.Execution.ReportResult = false;
TestContext.Current.Execution.AddLinkedCancellationToken(externalToken);
```

#### Metadata-Related Properties

**Before:**
```csharp
// ❌ Old - Direct access
var formatter = TestContext.Current.DisplayNameFormatter;
TestContext.Current.DisplayNameFormatter = typeof(MyFormatter);
```

**After:**
```csharp
// ✅ New - Through Metadata interface
var formatter = TestContext.Current.Metadata.DisplayNameFormatter;
TestContext.Current.Metadata.DisplayNameFormatter = typeof(MyFormatter);
```

#### Event Access

**Before:**
```csharp
// ❌ Old - Direct access
TestContext.Current.Events.OnTestStart += handler;
```

**After:**
```csharp
// ✅ New - Through Events interface (note: access is still the same, but now properly exposed through interface)
TestContext.Current.Events.OnTestStart += handler;
```

### Custom Hook Executors

If you're implementing custom hook executors that access these properties:

**Before:**
```csharp
public class MyHookExecutor : IHookExecutor
{
    public async Task ExecuteAsync(TestContext context, Func<Task> hookBody)
    {
        // ❌ Old - Direct property access
        if (context.ReportResult)
        {
            await hookBody();
        }
    }
}
```

**After:**
```csharp
public class MyHookExecutor : IHookExecutor
{
    public async Task ExecuteAsync(TestContext context, Func<Task> hookBody)
    {
        // ✅ New - Through Execution interface
        if (context.Execution.ReportResult)
        {
            await hookBody();
        }
    }
}
```

### Test Registration/Building

If you're setting custom hook executors during test registration:

**Before:**
```csharp
public class CustomTestBuilder
{
    public void ConfigureTest(TestContext context)
    {
        // ❌ Old - Direct property access
        context.CustomHookExecutor = new MyCustomExecutor();
        context.DisplayNameFormatter = typeof(MyFormatter);
    }
}
```

**After:**
```csharp
public class CustomTestBuilder
{
    public void ConfigureTest(TestContext context)
    {
        // ✅ New - Through appropriate interfaces
        context.Execution.CustomHookExecutor = new MyCustomExecutor();
        context.Metadata.DisplayNameFormatter = typeof(MyFormatter);
    }
}
```

### Cancellation Token Linking

**Before:**
```csharp
[Before(HookType.Test)]
public void Setup()
{
    var externalCts = new CancellationTokenSource();

    // ❌ Old - Direct method call
    TestContext.Current.AddLinkedCancellationToken(externalCts.Token);
}
```

**After:**
```csharp
[Before(HookType.Test)]
public void Setup()
{
    var externalCts = new CancellationTokenSource();

    // ✅ New - Through Execution interface
    TestContext.Current.Execution.AddLinkedCancellationToken(externalCts.Token);
}
```

## Benefits of the New Organization

### 1. Better Discoverability

IntelliSense now groups related functionality together, making it easier to find what you need:

```csharp
TestContext.Current.Execution.  // Shows only execution-related members
TestContext.Current.Metadata.   // Shows only metadata-related members
TestContext.Current.Output.     // Shows only output-related members
```

### 2. Clearer Intent

Code that accesses interface-specific properties communicates its intent more clearly:

```csharp
// Clear that we're dealing with execution lifecycle
context.Execution.OverrideResult(TestState.Passed, "Mocked result");

// Clear that we're configuring metadata
context.Metadata.DisplayName = "Custom Test Name";

// Clear that we're working with test output
context.Output.WriteLine("Debug information");
```

### 3. Interface Segregation Principle

Consumers can depend on specific interfaces instead of the full `TestContext`:

```csharp
// Before: Depends on entire TestContext
public class MyService
{
    public void ProcessTest(TestContext context) { }
}

// After: Depends only on what's needed
public class MyService
{
    public void ProcessTest(ITestMetadata metadata) { }
    public void HandleExecution(ITestExecution execution) { }
}
```

### 4. Zero-Allocation Design

The interface properties return `this` cast to the appropriate interface type, ensuring zero allocation overhead:

```csharp
// No new objects created - just interface casting
ITestExecution execution = testContext.Execution;  // Zero allocations
```

## Complete Interface Reference

### ITestExecution

Test execution state and lifecycle management:

```csharp
public interface ITestExecution
{
    TestPhase Phase { get; }
    TestResult? Result { get; }
    CancellationToken CancellationToken { get; }
    DateTimeOffset? TestStart { get; }
    DateTimeOffset? TestEnd { get; }
    int CurrentRetryAttempt { get; }
    string? SkipReason { get; }
    Func<TestContext, Exception, int, Task<bool>>? RetryFunc { get; }
    IHookExecutor? CustomHookExecutor { get; set; }
    bool ReportResult { get; set; }

    void OverrideResult(string reason);
    void OverrideResult(TestState state, string reason);
    void AddLinkedCancellationToken(CancellationToken cancellationToken);
}
```

### ITestMetadata

Test metadata and identity:

```csharp
public interface ITestMetadata
{
    Guid Id { get; }
    TestDetails TestDetails { get; }
    string TestName { get; }
    string DisplayName { get; set; }
    Type? DisplayNameFormatter { get; set; }
}
```

### ITestEvents

Test event integration:

```csharp
public interface ITestEvents
{
    TestContextEvents Events { get; }
}
```

### Other Interfaces

For completeness, here are the other interface properties available:

#### ITestOutput

```csharp
public interface ITestOutput
{
    void WriteLine(string message);
    void WriteError(string message);
    string GetOutput();
    string GetErrorOutput();
}
```

#### ITestParallelization

```csharp
public interface ITestParallelization
{
    // Parallelization configuration
}
```

#### ITestDependencies

```csharp
public interface ITestDependencies
{
    IEnumerable<TestContext> GetTests(Func<TestContext, bool> predicate);
    List<TestContext> GetTests(string testName);
    List<TestContext> GetTests(string testName, Type classType);
}
```

#### ITestStateBag

```csharp
public interface ITestStateBag
{
    ConcurrentDictionary<string, object?> ObjectBag { get; }
}
```

## Summary

The TestContext interface organization provides:
- ✅ **Better discoverability** through grouped functionality
- ✅ **Clearer code intent** with semantic interface names
- ✅ **Zero performance overhead** with allocation-free design
- ✅ **Backwards compatibility** with direct property access
- ✅ **Future flexibility** for interface-based dependencies

Update your code incrementally, starting with new code and high-value refactorings, while legacy code continues to work unchanged.
