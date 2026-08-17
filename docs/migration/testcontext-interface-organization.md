# TestContext Interface Organization Migration Guide

## Overview[​](#overview "Direct link to Overview")

TUnit has reorganized the `TestContext` API to provide a cleaner, more discoverable interface structure. Properties and methods are now organized into logical, focused interfaces that group related functionality together.

This migration guide helps you update code that directly accesses `TestContext` properties to use the new interface-based API.

## What Changed[​](#what-changed "Direct link to What Changed")

### New Interface Organization[​](#new-interface-organization "Direct link to New Interface Organization")

`TestContext` now exposes its API through focused interface properties:

```
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

### Property Reorganization[​](#property-reorganization "Direct link to Property Reorganization")

Several properties have been moved from the main `TestContext` class into their appropriate interfaces:

#### `ITestExecution` - Execution State and Lifecycle[​](#itestexecution---execution-state-and-lifecycle "Direct link to itestexecution---execution-state-and-lifecycle")

**New members:**

* `CustomHookExecutor` - Custom hook executor for test-level hooks
* `ReportResult` - Whether test results should be reported
* `AddLinkedCancellationToken()` - Link external cancellation tokens

**Existing members:**

* `Phase` - Current test phase (Discovery, Execution, Cleanup, etc.)
* `Result` - Test result after execution completes
* `CancellationToken` - Cancellation token for this test
* `TestStart` - Test execution start timestamp
* `TestEnd` - Test execution end timestamp
* `CurrentRetryAttempt` - Current retry attempt number
* `SkipReason` - Reason why test was skipped
* `RetryFunc` - Retry function for failed tests
* `OverrideResult()` - Override test result methods

#### `ITestMetadata` - Test Identity and Metadata[​](#itestmetadata---test-identity-and-metadata "Direct link to itestmetadata---test-identity-and-metadata")

**New member:**

* `DisplayNameFormatter` - Custom display name formatter type

**Existing members:**

* `TestDetails` - Detailed metadata about the test
* `TestName` - Base name of the test method
* `DisplayName` - Display name for the test (get/set)

**Note:** `Id` is now a public property directly on `TestContext`, not on `ITestMetadata`.

#### `ITestEvents` - Test Event Integration[​](#itestevents---test-event-integration "Direct link to itestevents---test-event-integration")

**New interface** exposing nullable event properties for lazy initialization:

* `OnDispose` - Event raised when test context is disposed
* `OnTestRegistered` - Event raised when test is registered
* `OnInitialize` - Event raised before test initialization
* `OnTestStart` - Event raised before test method execution
* `OnTestEnd` - Event raised after test method completion
* `OnTestSkipped` - Event raised when test is skipped
* `OnTestRetry` - Event raised before test retry

All events are nullable (`AsyncEvent<T>?`) to avoid allocating unused event handlers.

## Migration Steps[​](#migration-steps "Direct link to Migration Steps")

### Direct Property Access[​](#direct-property-access "Direct link to Direct Property Access")

If you were directly accessing properties on `TestContext`, they now need to be accessed through the appropriate interface property.

#### Execution-Related Properties[​](#execution-related-properties "Direct link to Execution-Related Properties")

**Before:**

```
// ❌ Old - Direct access

var customExecutor = TestContext.Current.CustomHookExecutor;

TestContext.Current.ReportResult = false;

TestContext.Current.AddLinkedCancellationToken(externalToken);
```

**After:**

```
// ✅ New - Through Execution interface

var customExecutor = TestContext.Current.Execution.CustomHookExecutor;

TestContext.Current.Execution.ReportResult = false;

TestContext.Current.Execution.AddLinkedCancellationToken(externalToken);
```

#### Metadata-Related Properties[​](#metadata-related-properties "Direct link to Metadata-Related Properties")

**Before:**

```
// ❌ Old - Direct access

var formatter = TestContext.Current.DisplayNameFormatter;

TestContext.Current.DisplayNameFormatter = typeof(MyFormatter);
```

**After:**

```
// ✅ New - Through Metadata interface

var formatter = TestContext.Current.Metadata.DisplayNameFormatter;

TestContext.Current.Metadata.DisplayNameFormatter = typeof(MyFormatter);
```

#### Event Access[​](#event-access "Direct link to Event Access")

Events are now accessed directly through the `Events` interface property, and all events are nullable for lazy initialization:

**Before:**

```
// ❌ Old - Accessing through a nested Events property

TestContext.Current.Events.OnTestStart += handler;
```

**After:**

```
// ✅ New - Direct access to nullable event properties

TestContext.Current.Events.OnTestStart += handler;



// Events are nullable and lazily initialized

if (TestContext.Current.Events.OnTestStart != null)

{

    await TestContext.Current.Events.OnTestStart.InvokeAsync(testContext, testContext);

}
```

### Custom Hook Executors[​](#custom-hook-executors "Direct link to Custom Hook Executors")

If you're implementing custom hook executors that access these properties:

**Before:**

```
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

```
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

### Test Registration/Building[​](#test-registrationbuilding "Direct link to Test Registration/Building")

If you're setting custom hook executors during test registration:

**Before:**

```
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

```
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

### Cancellation Token Linking[​](#cancellation-token-linking "Direct link to Cancellation Token Linking")

**Before:**

```
[Before(Test)]

public void Setup()

{

    var externalCts = new CancellationTokenSource();



    // ❌ Old - Direct method call

    TestContext.Current.AddLinkedCancellationToken(externalCts.Token);

}
```

**After:**

```
[Before(Test)]

public void Setup()

{

    var externalCts = new CancellationTokenSource();



    // ✅ New - Through Execution interface

    TestContext.Current.Execution.AddLinkedCancellationToken(externalCts.Token);

}
```

## Benefits of the New Organization[​](#benefits-of-the-new-organization "Direct link to Benefits of the New Organization")

### 1. Better Discoverability[​](#1-better-discoverability "Direct link to 1. Better Discoverability")

IntelliSense now groups related functionality together, making it easier to find what you need:

```
TestContext.Current.Execution.  // Shows only execution-related members

TestContext.Current.Metadata.   // Shows only metadata-related members

TestContext.Current.Output.     // Shows only output-related members
```

### 2. Clearer Intent[​](#2-clearer-intent "Direct link to 2. Clearer Intent")

Code that accesses interface-specific properties communicates its intent more clearly:

```
// Clear that we're dealing with execution lifecycle

context.Execution.OverrideResult(TestState.Passed, "Mocked result");



// Clear that we're configuring metadata

context.Metadata.DisplayName = "Custom Test Name";



// Clear that we're working with test output

context.Output.WriteLine("Debug information");
```

### 3. Interface Segregation Principle[​](#3-interface-segregation-principle "Direct link to 3. Interface Segregation Principle")

Consumers can depend on specific interfaces instead of the full `TestContext`:

```
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

### 4. Zero-Allocation Design[​](#4-zero-allocation-design "Direct link to 4. Zero-Allocation Design")

The interface properties return `this` cast to the appropriate interface type, ensuring zero allocation overhead:

```
// No new objects created - just interface casting

ITestExecution execution = testContext.Execution;  // Zero allocations
```

## Complete Interface Reference[​](#complete-interface-reference "Direct link to Complete Interface Reference")

### ITestExecution[​](#itestexecution "Direct link to ITestExecution")

Test execution state and lifecycle management:

```
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

    bool IsNotDiscoverable { get; set; }



    void OverrideResult(TestState state, string reason);

    void AddLinkedCancellationToken(CancellationToken cancellationToken);

}
```

### ITestMetadata[​](#itestmetadata "Direct link to ITestMetadata")

Test metadata and identity:

```
public interface ITestMetadata

{

    string DefinitionId { get; }

    TestDetails TestDetails { get; }

    string TestName { get; }

    string DisplayName { get; set; }

    Type? DisplayNameFormatter { get; set; }

}
```

**Note:** `DefinitionId` identifies the test definition (template/source) and is shared across all instances of parameterized tests. The per-instance `Id` is a direct property on `TestContext` (see above).

### ITestEvents[​](#itestevents "Direct link to ITestEvents")

Test event integration with nullable lazy-initialized event properties:

```
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

### Other Interfaces[​](#other-interfaces "Direct link to Other Interfaces")

For completeness, here are the other interface properties available:

#### ITestOutput[​](#itestoutput "Direct link to ITestOutput")

```
public interface ITestOutput

{

    TextWriter StandardOutput { get; }

    TextWriter ErrorOutput { get; }

    IReadOnlyCollection<Artifact> Artifacts { get; }



    void AttachArtifact(Artifact artifact);

    void AttachArtifact(string filePath, string? displayName = null, string? description = null);

    string GetStandardOutput();

    string GetErrorOutput();

    void WriteLine(string message);

    void WriteError(string message);

}
```

#### ITestParallelization[​](#itestparallelization "Direct link to ITestParallelization")

```
public interface ITestParallelization

{

    IReadOnlyList<IParallelConstraint> Constraints { get; }

    Priority ExecutionPriority { get; set; }

    IParallelLimit? Limiter { get; }  // Read-only - use TestRegisteredContext to set

    void AddConstraint(IParallelConstraint constraint);

}
```

**Important:** The `Limiter` property is **read-only** on the public interface. To set the parallel limiter, use the phase-specific `TestRegisteredContext.SetParallelLimiter()` method during test registration:

```
[TestRegistered]

public static void OnTestRegistered(TestRegisteredContext context)

{

    // ✅ Correct - Use phase-specific context

    context.SetParallelLimiter(new ParallelLimit3());

}
```

#### ITestDependencies[​](#itestdependencies "Direct link to ITestDependencies")

```
public interface ITestDependencies

{

    IReadOnlyList<TestDetails> DependsOn { get; }

    string? ParentTestId { get; }

    TestRelationship Relationship { get; }



    IReadOnlyList<TestContext> GetTests(Func<TestContext, bool> predicate);

    IReadOnlyList<TestContext> GetTests(string testName);

    IReadOnlyList<TestContext> GetTests(string testName, Type classType);

}
```

**Changed:** All `GetTests` methods now return `IReadOnlyList<TestContext>` for consistency and to better express the immutable nature of the returned collection.

#### ITestStateBag[​](#iteststatebag "Direct link to ITestStateBag")

```
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

## Summary[​](#summary "Direct link to Summary")

The TestContext interface organization provides:

* ✅ **Better discoverability** through grouped functionality
* ✅ **Clearer code intent** with semantic interface names
* ✅ **Zero performance overhead** with allocation-free design
* ✅ **Backwards compatibility** with direct property access
* ✅ **Future flexibility** for interface-based dependencies

Update your code incrementally, starting with new code and high-value refactorings, while legacy code continues to work unchanged.
