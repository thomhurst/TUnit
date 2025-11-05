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

    // Note: Services property is internal - use dependency injection instead
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
- `TestDetails` - Detailed metadata about the test
- `TestName` - Base name of the test method
- `DisplayName` - Display name for the test (get/set)

**Note:** `Id` is now a public property directly on `TestContext`, not on `ITestMetadata`.

#### `ITestEvents` - Test Event Integration

**New interface** exposing nullable event properties for lazy initialization:
- `OnDispose` - Event raised when test context is disposed
- `OnTestRegistered` - Event raised when test is registered
- `OnInitialize` - Event raised before test initialization
- `OnTestStart` - Event raised before test method execution
- `OnTestEnd` - Event raised after test method completion
- `OnTestSkipped` - Event raised when test is skipped
- `OnTestRetry` - Event raised before test retry

All events are nullable (`AsyncEvent<T>?`) to avoid allocating unused event handlers.

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

Events are now accessed directly through the `Events` interface property, and all events are nullable for lazy initialization:

**Before:**
```csharp
// ❌ Old - Accessing through a nested Events property
TestContext.Current.Events.OnTestStart += handler;
```

**After:**
```csharp
// ✅ New - Direct access to nullable event properties
TestContext.Current.Events.OnTestStart += handler;

// Events are nullable and lazily initialized
if (TestContext.Current.Events.OnTestStart != null)
{
    await TestContext.Current.Events.OnTestStart.InvokeAsync(testContext, testContext);
}
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
    TestDetails TestDetails { get; }
    string TestName { get; }
    string DisplayName { get; set; }
    Type? DisplayNameFormatter { get; set; }
}
```

**Note:** `Id` is available only through the `ITestMetadata` interface (accessed via `TestContext.Metadata.Id`), not as a direct property on `TestContext`.

### ITestEvents

Test event integration with nullable lazy-initialized event properties:

```csharp
public interface ITestEvents
{
    AsyncEvent<TestContext>? OnDispose { get; }
    AsyncEvent<TestContext>? OnTestRegistered { get; }
    AsyncEvent<TestContext>? OnInitialize { get; }
    AsyncEvent<TestContext>? OnTestStart { get; }
    AsyncEvent<TestContext>? OnTestEnd { get; }
    AsyncEvent<TestContext>? OnTestSkipped { get; }
    AsyncEvent<(TestContext TestContext, int RetryAttempt)>? OnTestRetry { get; }
}
```

**Important:** All event properties are nullable to enable lazy initialization. Events are only allocated when subscribers are added, avoiding unnecessary allocations for unused events.

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
    IReadOnlyList<IParallelConstraint> Constraints { get; }
    Priority ExecutionPriority { get; set; }
    IParallelLimit? Limiter { get; }  // Read-only - use TestRegisteredContext to set
    void AddConstraint(IParallelConstraint constraint);
}
```

**Important:** The `Limiter` property is **read-only** on the public interface. To set the parallel limiter, use the phase-specific `TestRegisteredContext.SetParallelLimiter()` method during test registration:

```csharp
[TestRegistered]
public static void OnTestRegistered(TestRegisteredContext context)
{
    // ✅ Correct - Use phase-specific context
    context.SetParallelLimiter(new ParallelLimit3());
}
```

#### ITestDependencies

```csharp
public interface ITestDependencies
{
    IReadOnlyList<TestContext> GetTests(Func<TestContext, bool> predicate);
    IReadOnlyList<TestContext> GetTests(string testName);
    IReadOnlyList<TestContext> GetTests(string testName, Type classType);
}
```

**Changed:** All `GetTests` methods now return `IReadOnlyList<TestContext>` for consistency and to better express the immutable nature of the returned collection.

#### ITestStateBag

```csharp
public interface ITestStateBag
{
    ConcurrentDictionary<string, object?> Items { get; }
    object? this[string key] { get; set; }
    int Count { get; }
    bool ContainsKey(string key);
    T GetOrAdd<T>(string key, Func<string, T> valueFactory);
    bool TryGetValue<T>(string key, out T value);
    bool TryRemove(string key, out object? value);
}
```

The `StateBag` interface provides both direct dictionary access via `Items` and type-safe helper methods for common operations.

## Summary

The TestContext interface organization provides:
- ✅ **Better discoverability** through grouped functionality
- ✅ **Clearer code intent** with semantic interface names
- ✅ **Zero performance overhead** with allocation-free design
- ✅ **Backwards compatibility** with direct property access
- ✅ **Future flexibility** for interface-based dependencies

Update your code incrementally, starting with new code and high-value refactorings, while legacy code continues to work unchanged.
