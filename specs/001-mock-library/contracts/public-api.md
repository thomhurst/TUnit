# Public API Contract: TUnit.Mocks

**Branch**: `001-mock-library` | **Date**: 2026-02-20

This document defines the public API surface of `TUnit.Mocks`. All types in this contract are part of the stable public API and subject to semantic versioning.

## Namespace: TUnit.Mocks

### Static Entry Point

```csharp
public static class Mock
{
    /// <summary>Creates a mock of T in loose mode.</summary>
    public static Mock<T> Of<T>() where T : class;

    /// <summary>Creates a mock of T with specified behavior.</summary>
    public static Mock<T> Of<T>(MockBehavior behavior) where T : class;

    /// <summary>Creates a mock implementing two interfaces.</summary>
    public static Mock<T1> Of<T1, T2>() where T1 : class where T2 : class;

    /// <summary>Creates a mock implementing three interfaces.</summary>
    public static Mock<T1> Of<T1, T2, T3>() where T1 : class where T2 : class where T3 : class;

    /// <summary>Creates a mock implementing four interfaces.</summary>
    public static Mock<T1> Of<T1, T2, T3, T4>() where T1 : class where T2 : class where T3 : class where T4 : class;

    /// <summary>Creates a partial mock of T (spy) — base methods call through.</summary>
    public static Mock<T> OfPartial<T>(params object[] constructorArgs) where T : class;

    /// <summary>Creates a mock delegate.</summary>
    public static Mock<TDelegate> OfDelegate<TDelegate>() where TDelegate : Delegate;

    /// <summary>Verifies calls across multiple mocks happened in order.</summary>
    public static void VerifyInOrder(Action verification);
}
```

### Non-Generic Mock Interface

```csharp
/// <summary>Common interface for batch operations via MockRepository.</summary>
public interface IMock
{
    /// <summary>Verifies all registered setups were invoked at least once.</summary>
    void VerifyAll();

    /// <summary>Fails if any call was not matched by a prior verification.</summary>
    void VerifyNoOtherCalls();

    /// <summary>Clears all setups and call history.</summary>
    void Reset();
}
```

### Mock Wrapper

```csharp
public sealed class Mock<T> : IMock where T : class
{
    /// <summary>The mock object that implements T.</summary>
    public T Object { get; }

    /// <summary>Generated setup surface — mirrors T's members with Arg<> parameters.</summary>
    public IMockSetup<T> Setup { get; }

    /// <summary>Generated verification surface — mirrors T's members with Arg<> parameters.</summary>
    public IMockVerify<T>? Verify { get; }

    /// <summary>Generated event-raising surface (present only if T has events).</summary>
    public IMockRaise<T>? Raise { get; }

    /// <summary>All calls made to this mock, in order.</summary>
    public IReadOnlyList<CallRecord> Invocations { get; }

    /// <summary>Clears all setups and call history.</summary>
    public void Reset();

    /// <summary>Verifies all registered setups were invoked at least once.</summary>
    public void VerifyAll();

    /// <summary>Fails if any recorded call was not matched by a prior verification.</summary>
    public void VerifyNoOtherCalls();

    /// <summary>Enables auto-tracking for all properties.</summary>
    public void SetupAllProperties();

    /// <summary>Implicit conversion to T — no .Object needed.</summary>
    public static implicit operator T(Mock<T> mock);

    /// <summary>Sets the current state for state machine mocking.</summary>
    public void SetState(string? stateName);

    /// <summary>Configures setups scoped to a specific state.</summary>
    public void InState(string stateName, Action<IMockSetup<T>> configure);

    /// <summary>Returns diagnostics about setup coverage and call matching.</summary>
    public MockDiagnostics GetDiagnostics();

    /// <summary>Gets or sets the custom default value provider.</summary>
    public IDefaultValueProvider? DefaultValueProvider { get; set; }
}
```

### Mock Repository

```csharp
/// <summary>Tracks multiple mocks for batch operations.</summary>
public sealed class MockRepository
{
    /// <summary>Creates and tracks a mock of T.</summary>
    public Mock<T> Of<T>() where T : class;

    /// <summary>Creates and tracks a mock of T with specified behavior.</summary>
    public Mock<T> Of<T>(MockBehavior behavior) where T : class;

    /// <summary>Verifies all setups on all tracked mocks were invoked.</summary>
    public void VerifyAll();

    /// <summary>Fails if any tracked mock has unverified calls.</summary>
    public void VerifyNoOtherCalls();

    /// <summary>Resets all tracked mocks.</summary>
    public void Reset();
}
```

### Mock Behavior

```csharp
public enum MockBehavior
{
    /// <summary>Unconfigured calls return smart defaults. (Default)</summary>
    Loose = 0,

    /// <summary>Unconfigured calls throw MockStrictBehaviorException.</summary>
    Strict = 1
}
```

## Namespace: TUnit.Mocks.Arguments

### Argument Matcher Factory

```csharp
public static class Arg
{
    /// <summary>Matches any value of T.</summary>
    public static Arg<T> Any<T>();

    /// <summary>Matches a specific value via equality.</summary>
    public static Arg<T> Is<T>(T value);

    /// <summary>Matches values satisfying a predicate.</summary>
    public static Arg<T> Is<T>(Func<T, bool> predicate);

    /// <summary>Matches null values.</summary>
    public static Arg<T> IsNull<T>() where T : class;

    /// <summary>Matches non-null values.</summary>
    public static Arg<T> IsNotNull<T>() where T : class;

    /// <summary>Creates an argument capture.</summary>
    public static ArgCapture<T> Capture<T>();

    /// <summary>Specifies an out parameter value for setup.</summary>
    public static T Out<T>(T value);

    /// <summary>Specifies a ref parameter matcher for setup.</summary>
    public static Arg<T> Ref<T>(T value);

    /// <summary>Matches a string against a regex pattern.</summary>
    public static Arg<string> Matches(string pattern);

    /// <summary>Matches a string against a compiled Regex.</summary>
    public static Arg<string> Matches(Regex regex);

    /// <summary>Matches using a user-defined custom matcher.</summary>
    public static Arg<T> Matches<T>(IArgumentMatcher<T> matcher);

    /// <summary>Matches a collection containing the specified item.</summary>
    public static Arg<IEnumerable<T>> Contains<T>(T item);

    /// <summary>Matches a collection with the specified count.</summary>
    public static Arg<IEnumerable<T>> HasCount<T>(int count);

    /// <summary>Matches an empty collection.</summary>
    public static Arg<IEnumerable<T>> IsEmpty<T>();

    /// <summary>Matches a collection with element-by-element equality.</summary>
    public static Arg<IEnumerable<T>> SequenceEquals<T>(IEnumerable<T> expected);
}
```

### Argument Matcher Wrapper

```csharp
public readonly struct Arg<T>
{
    /// <summary>Implicit conversion from T — creates an exact-match matcher.</summary>
    public static implicit operator Arg<T>(T value);

    /// <summary>Implicit conversion from ArgCapture<T>.</summary>
    public static implicit operator Arg<T>(ArgCapture<T> capture);
}
```

### Argument Capture

```csharp
public sealed class ArgCapture<T>
{
    /// <summary>All captured argument values in call order.</summary>
    public IReadOnlyList<T> Values { get; }

    /// <summary>The most recently captured value.</summary>
    public T Latest { get; }
}
```

### Custom Matcher Interface

```csharp
public interface IArgumentMatcher
{
    /// <summary>Returns true if the actual value matches.</summary>
    bool Matches(object? value);

    /// <summary>Human-readable description for failure messages.</summary>
    string Describe();
}

public interface IArgumentMatcher<in T> : IArgumentMatcher
{
    /// <summary>Strongly-typed match.</summary>
    bool Matches(T? value);
}
```

## Namespace: TUnit.Mocks.Setup

### Method Setup Builder

```csharp
public interface IMethodSetup<TReturn>
{
    /// <summary>Configure a fixed return value. Auto-wraps for async methods.</summary>
    ISetupChain<TReturn> Returns(TReturn value);

    /// <summary>Configure a computed return value from arguments.</summary>
    ISetupChain<TReturn> Returns(Func<TReturn> factory);

    /// <summary>Configure sequential return values.</summary>
    ISetupChain<TReturn> ReturnsSequentially(params TReturn[] values);

    /// <summary>Configure an exception to throw. For async methods, returns faulted task.</summary>
    ISetupChain<TReturn> Throws<TException>() where TException : Exception, new();

    /// <summary>Configure a specific exception instance to throw.</summary>
    ISetupChain<TReturn> Throws(Exception exception);

    /// <summary>Execute a callback when the method is called.</summary>
    ISetupChain<TReturn> Callback(Action callback);
}

/// <summary>Void method setup — no return value configuration.</summary>
public interface IVoidMethodSetup
{
    IVoidSetupChain Throws<TException>() where TException : Exception, new();
    IVoidSetupChain Throws(Exception exception);
    IVoidSetupChain Callback(Action callback);
}
```

### Setup Chaining (Sequential Behaviors)

```csharp
public interface ISetupChain<TReturn>
{
    /// <summary>Chain the next call's behavior.</summary>
    IMethodSetup<TReturn> Then();

    /// <summary>Transition to a named state after this behavior executes.</summary>
    ISetupChain<TReturn> TransitionsTo(string stateName);

    /// <summary>Assign a value to an out/ref parameter.</summary>
    ISetupChain<TReturn> SetsOutParameter(int paramIndex, object? value);

    /// <summary>Auto-raise a named event after this behavior executes.</summary>
    ISetupChain<TReturn> Raises(string eventName, object? args = null);
}

public interface IVoidSetupChain
{
    /// <summary>Chain the next call's behavior.</summary>
    IVoidMethodSetup Then();

    /// <summary>Transition to a named state after this behavior executes.</summary>
    IVoidSetupChain TransitionsTo(string stateName);

    /// <summary>Assign a value to an out/ref parameter.</summary>
    IVoidSetupChain SetsOutParameter(int paramIndex, object? value);

    /// <summary>Auto-raise a named event after this behavior executes.</summary>
    IVoidSetupChain Raises(string eventName, object? args = null);
}
```

### Property Setup

```csharp
public interface IPropertySetup<T>
{
    /// <summary>Configure the property getter to return a value.</summary>
    void Returns(T value);

    /// <summary>Track property setter calls for verification.</summary>
    void TrackSets();
}
```

## Namespace: TUnit.Mocks.Verification

### Call Verification

```csharp
public interface ICallVerification
{
    /// <summary>Verify the method was called the expected number of times.</summary>
    void WasCalled(Times times);

    /// <summary>Shorthand for WasCalled(Times.Never).</summary>
    void WasNeverCalled();
}
```

### Property Verification

```csharp
public interface IPropertyVerification<T>
{
    /// <summary>Verify property getter was called.</summary>
    void GetWasCalled(Times times);

    /// <summary>Verify property was set to a specific value.</summary>
    void WasSetTo(Arg<T> value);

    /// <summary>Verify property was set any number of times.</summary>
    void SetWasCalled(Times times);
}
```

### Times

```csharp
public readonly struct Times : IEquatable<Times>
{
    public static Times Once { get; }
    public static Times Never { get; }
    public static Times Exactly(int n);
    public static Times AtLeast(int n);
    public static Times AtMost(int n);
    public static Times Between(int min, int max);
}
```

## Namespace: TUnit.Mocks.Exceptions

```csharp
/// <summary>Thrown when mock verification fails (call count mismatch).</summary>
public class MockVerificationException : Exception
{
    public string ExpectedCall { get; }
    public int ExpectedCount { get; }
    public int ActualCount { get; }
    public IReadOnlyList<string> ActualCalls { get; }
}

/// <summary>Thrown when an unconfigured method is called in strict mode.</summary>
public class MockStrictBehaviorException : Exception
{
    public string UnconfiguredCall { get; }
}
```

## Namespace: TUnit.Mocks.Diagnostics

```csharp
/// <summary>Diagnostic report for a mock instance.</summary>
public sealed record MockDiagnostics(
    IReadOnlyList<SetupInfo> UnusedSetups,
    IReadOnlyList<CallRecord> UnmatchedCalls,
    int TotalSetups,
    int ExercisedSetups
);

/// <summary>Metadata about a registered setup.</summary>
public sealed record SetupInfo(
    int MemberId,
    string MemberName,
    string[] MatcherDescriptions,
    int InvokeCount
);
```

## Package: TUnit.Mocks.Assertions (separate NuGet — bridges TUnit.Mocks + TUnit.Assertions)

```csharp
/// <summary>Extension methods for TUnit assertion integration.</summary>
/// <remarks>Requires: dotnet add package TUnit.Mocks.Assertions</remarks>
public static class MockAssertionExtensions
{
    /// <summary>Assert that a method was called the expected number of times.</summary>
    public static WasCalledAssertion WasCalled(
        this IAssertionSource<ICallVerification> source, Times times);

    /// <summary>Assert that a method was called at least once.</summary>
    public static WasCalledAssertion WasCalled(
        this IAssertionSource<ICallVerification> source);

    /// <summary>Assert that a method was never called.</summary>
    public static WasNeverCalledAssertion WasNeverCalled(
        this IAssertionSource<ICallVerification> source);
}
```

## Namespace: TUnit.Mocks.Defaults

```csharp
/// <summary>Custom default value provider for unconfigured methods.</summary>
public interface IDefaultValueProvider
{
    /// <summary>Returns true if this provider can supply a default for the given type.</summary>
    bool CanProvide(Type type);

    /// <summary>Returns the default value for the given type.</summary>
    object? GetDefaultValue(Type type);
}
```

## Generated Types (Per Mock Target)

For `Mock.Of<IFoo>()`, the source generator produces (in user's compilation):

```csharp
// Setup surface — one method per IFoo member, returns IMethodSetup<TReturn>
[GeneratedCode("TUnit.Mocks.SourceGenerator")]
public sealed class IFoo_MockSetup
{
    // For: int Add(int a, int b)
    public IMethodSetup<int> Add(Arg<int> a, Arg<int> b);

    // For: string Name { get; set; }
    public IPropertySetup<string> Name { get; }

    // For: void Process(string item)
    public IVoidMethodSetup Process(Arg<string> item);

    // For: Task<string> FetchAsync(string key)
    public IMethodSetup<string> FetchAsync(Arg<string> key); // Note: unwrapped return type
}

// Verification surface — one method per IFoo member, returns ICallVerification
[GeneratedCode("TUnit.Mocks.SourceGenerator")]
public sealed class IFoo_MockVerify
{
    public ICallVerification Add(Arg<int> a, Arg<int> b);
    public IPropertyVerification<string> Name { get; }
    public ICallVerification Process(Arg<string> item);
    public ICallVerification FetchAsync(Arg<string> key);
}

// Event-raising surface (only if IFoo has events)
[GeneratedCode("TUnit.Mocks.SourceGenerator")]
public sealed class IFoo_MockRaise
{
    // For: event EventHandler<string> OnMessage
    public void OnMessage(string args);
}
```
