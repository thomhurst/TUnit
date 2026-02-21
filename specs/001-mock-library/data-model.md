# Data Model: TUnit.Mocks

**Branch**: `001-mock-library` | **Date**: 2026-02-20

## Core Types

### Mock<T> (Public — User-Facing Wrapper)

The central type users interact with. Wraps a generated mock implementation and exposes control surfaces.

- **Fields**:
  - `T Object` — the generated mock instance (implements T)
  - `MockBehavior Behavior` — Loose (default) or Strict
  - `IMockSetup<T> Setup` — generated strongly-typed setup surface
  - `IMockVerify<T> Verify` — generated strongly-typed verification surface
  - `IMockRaise<T> Raise` — generated event-raising surface (only if T has events)
  - `MockEngine<T> Engine` — internal engine managing setups, calls, behaviors

- **Methods**:
  - `Reset()` — clears all setups and call history
  - `implicit operator T` — implicit conversion to the mocked type

- **Relationships**:
  - Contains one `MockEngine<T>`
  - Contains one generated `T_MockImpl` (the concrete implementation)
  - Contains one generated `T_MockSetup`
  - Contains one generated `T_MockVerify`
  - Contains one generated `T_MockRaise` (conditional on events)

### MockEngine<T> (Internal — Core Logic)

Manages setup storage, call recording, and call dispatching. One per mock instance.

- **Fields**:
  - `List<MethodSetup> Setups` — all registered setups, protected by `ReaderWriterLockSlim`
  - `ConcurrentQueue<CallRecord> CallHistory` — all recorded calls
  - `MockBehavior Behavior` — inherited from Mock<T>

- **Methods**:
  - `HandleCall(int memberId, object?[] args)` → resolves matching setup, records call, returns result
  - `HandleCallWithReturn<TReturn>(int memberId, object?[] args)` → same with return value
  - `AddSetup(MethodSetup setup)` — thread-safe setup registration
  - `GetCallsFor(int memberId)` → filters call history by member
  - `Reset()` — clears setups and call history

### MethodSetup (Internal — Single Setup Configuration)

Represents a configured behavior for a specific method call pattern.

- **Fields**:
  - `int MemberId` — identifies which method/property this setup applies to
  - `IArgumentMatcher[] Matchers` — one matcher per parameter
  - `List<IBehavior> Behaviors` — chain of behaviors (sequential)
  - `int CallIndex` — current position in the behavior chain (atomic)

- **Methods**:
  - `Matches(object?[] actualArgs)` → evaluates all matchers against actual arguments
  - `Execute()` → returns the current behavior and advances the chain index

### IBehavior (Internal — Behavior Interface)

Base interface for all setup behaviors.

- **Implementations**:
  - `ReturnBehavior<T>` — returns a fixed value
  - `ThrowBehavior` — throws a configured exception
  - `CallbackBehavior` — executes a delegate
  - `ComputedReturnBehavior<T>` — computes return value from arguments via delegate

### CallRecord (Internal — Recorded Call)

Immutable record of a single call made to a mocked member.

- **Fields**:
  - `int MemberId` — which method was called
  - `string MemberName` — human-readable method name (for error messages)
  - `object?[] Arguments` — snapshot of argument values at call time
  - `DateTime Timestamp` — when the call was made

### IArgumentMatcher (Public — Matcher Interface)

Interface for argument matching. Users implement this for custom matchers.

- **Methods**:
  - `bool Matches(object? value)` — returns true if the actual value matches

- **Built-in Implementations**:
  - `AnyMatcher<T>` — always returns true
  - `ExactMatcher<T>` — equality comparison
  - `PredicateMatcher<T>` — delegates to `Func<T, bool>`
  - `NullMatcher<T>` — matches only null
  - `NotNullMatcher<T>` — matches any non-null
  - `CaptureMatcher<T>` — matches any value and records it

### Arg<T> (Public — Matcher Wrapper)

Strongly-typed wrapper around `IArgumentMatcher`. Has implicit conversion from `T` (creates `ExactMatcher<T>`).

- **Fields**:
  - `IArgumentMatcher Matcher` — the underlying matcher

- **Static Factory Methods** (on `Arg` static class):
  - `Arg.Any<T>()` → `Arg<T>` wrapping `AnyMatcher<T>`
  - `Arg.Is<T>(T value)` → `Arg<T>` wrapping `ExactMatcher<T>`
  - `Arg.Is<T>(Func<T, bool> predicate)` → `Arg<T>` wrapping `PredicateMatcher<T>`
  - `Arg.IsNull<T>()` → `Arg<T>` wrapping `NullMatcher<T>`
  - `Arg.IsNotNull<T>()` → `Arg<T>` wrapping `NotNullMatcher<T>`
  - `Arg.Capture<T>()` → `ArgCapture<T>` wrapping `CaptureMatcher<T>`

### ArgCapture<T> (Public — Argument Capture)

Stores captured argument values for later inspection.

- **Fields**:
  - `IReadOnlyList<T> Values` — all captured values in call order

### IMethodSetup<TReturn> (Public — Fluent Setup Builder)

Returned by `.Setup.MethodName(args)`. Enables fluent configuration chaining.

- **Methods**:
  - `Returns(TReturn value)` → configures fixed return (auto-wraps for async methods)
  - `Returns<TArgs...>(Func<TArgs..., TReturn> factory)` → computed return
  - `ReturnsSequentially(params TReturn[] values)` → sequential returns
  - `Throws<TException>()` → configures exception throw
  - `Throws(Exception exception)` → configures specific exception instance
  - `Callback(Action<TArgs...> callback)` → executes delegate on call
  - `Then()` → returns a new `IMethodSetup<TReturn>` for chaining the next call's behavior

### ICallVerification (Public — Verification Builder)

Returned by `.Verify.MethodName(args)`. Enables fluent call count verification.

- **Methods**:
  - `WasCalled(Times times)` → verifies call count, throws `MockVerificationException` on failure
  - `WasNeverCalled()` → shorthand for `WasCalled(Times.Never)`

### Times (Public — Call Count Specification)

Value type specifying expected call counts.

- **Static Factory Methods**:
  - `Times.Once` → exactly 1
  - `Times.Never` → exactly 0
  - `Times.Exactly(int n)` → exactly n
  - `Times.AtLeast(int n)` → >= n
  - `Times.AtMost(int n)` → <= n
  - `Times.Between(int min, int max)` → min..max inclusive

### MockBehavior (Public — Enum)

- `Loose` — unconfigured calls return smart defaults (default)
- `Strict` — unconfigured calls throw `MockStrictBehaviorException`

### Exceptions (Public)

- `MockVerificationException` : `Exception` — thrown when `.WasCalled()` fails
- `MockStrictBehaviorException` : `Exception` — thrown on unconfigured call in strict mode

### IMock (Public — Non-Generic Mock Interface)

Common interface for `Mock<T>` enabling batch operations. Used by `MockRepository`.

- **Methods**:
  - `VerifyAll()` — fails if any registered setup was never invoked
  - `VerifyNoOtherCalls()` — fails if any call was not matched by a prior verification
  - `Reset()` — clears all setups and call history

### MockRepository (Public — Batch Mock Manager)

Tracks multiple mocks and provides batch operations.

- **Fields**:
  - `List<IMock> TrackedMocks` — internal list of all mocks created through this repository

- **Methods**:
  - `Mock<T> Of<T>()` — creates and tracks a mock
  - `Mock<T> Of<T>(MockBehavior behavior)` — creates and tracks a mock with behavior
  - `VerifyAll()` — calls VerifyAll on all tracked mocks, aggregates failures
  - `VerifyNoOtherCalls()` — calls VerifyNoOtherCalls on all tracked mocks
  - `Reset()` — resets all tracked mocks

### RegexMatcher (Internal — Regex String Matcher)

Matches string arguments against a regular expression pattern.

- **Fields**:
  - `Regex Pattern` — compiled regex
- **Methods**:
  - `Matches(object? value)` — returns true if string value matches pattern
  - `Describe()` — returns `"Arg.Matches(/pattern/)"` for error messages

### Collection Matchers (Internal)

- `ContainsMatcher<T>` — matches collections containing a specific item
- `CountMatcher` — matches collections with a specific count
- `EmptyMatcher` — matches empty collections
- `SequenceEqualsMatcher<T>` — matches collections with element-by-element equality

## Entity Relationship Diagram

```
Mock<T> : IMock
 ├── T_MockImpl : T [, T2, T3, T4]  (generated, implements all interfaces)
 ├── T_MockSetup : IMockSetup<T>     (generated, mirrors T with Arg<> params)
 │    └── T_MockSetupExtensions      (generated extension methods)
 ├── T_MockVerify : IMockVerify<T>   (generated, mirrors T with Arg<> params)
 │    └── T_MockVerifyExtensions     (generated extension methods)
 ├── T_MockRaise : IMockRaise<T>     (generated, methods per event on T)
 │    └── T_MockRaiseExtensions      (generated extension methods)
 ├── Invocations → IReadOnlyList<CallRecord>  (public call history)
 └── MockEngine<T>
      ├── List<MethodSetup>    (setup store, with InvokeCount tracking)
      │    ├── IArgumentMatcher[] (per parameter)
      │    │    ├── RegexMatcher
      │    │    ├── ContainsMatcher<T>
      │    │    ├── CountMatcher
      │    │    ├── EmptyMatcher
      │    │    └── SequenceEqualsMatcher<T>
      │    └── List<IBehavior>   (behavior chain)
      ├── ConcurrentQueue<CallRecord>  (call history, with IsVerified flag)
      └── Dictionary<int, object?>     (auto-track property store)

MockRepository
 └── List<IMock>  (tracked mocks for batch operations)
```

## Generated Types (Per Mocked Type)

For each unique `T` detected in `Mock.Of<T>()`:

| Generated Type | Purpose | Visibility |
|---------------|---------|------------|
| `T_MockImpl` | Concrete implementation of T, routes calls through MockEngine | internal |
| `T_MockSetup` | Setup surface mirroring T's API with `Arg<>` params | public |
| `T_MockVerify` | Verification surface mirroring T's API with `Arg<>` params | public |
| `T_MockRaise` | Event-raising surface (only if T declares events) | public |
| `T_MockFactory` | Static factory registered for `Mock.Of<T>()` resolution | internal |

## New Types (Beyond-Parity Features)

### State Machine Types

**MethodSetup** additions:
- `RequiredState: string?` — if non-null, setup only matches when engine state equals this value
- `TransitionTarget: string?` — if non-null, engine transitions to this state after behavior executes

**MockEngine<T>** additions:
- `_currentState: string?` — current state name, checked in `FindMatchingSetup`
- `PendingRequiredState: string?` — temporary state for scoped `InState()` setup
- `TransitionTo(string?)` — updates `_currentState`

### Mock Diagnostics Types

```
MockDiagnostics (public, readonly record)
├── UnusedSetups: IReadOnlyList<SetupInfo>      — setups with InvokeCount == 0
├── UnmatchedCalls: IReadOnlyList<CallRecord>   — calls that matched no setup
├── TotalSetups: int
└── ExercisedSetups: int

SetupInfo (public, readonly record)
├── MemberId: int
├── MemberName: string
├── MatcherDescriptions: string[]
└── InvokeCount: int
```

### Assertion Integration Types

```
WasCalledAssertion : Assertion<ICallVerification>
├── _times: Times
├── CheckAsync() → AssertionResult
└── GetExpectation() → string

WasNeverCalledAssertion : Assertion<ICallVerification>
├── CheckAsync() → AssertionResult
└── GetExpectation() → string
```

**ICallVerification** additions:
- `CheckWasCalled(Times) → AssertionResult` — non-throwing verification check
- `CheckWasNeverCalled() → AssertionResult` — non-throwing never-called check

### Typed Callback Generated Types

For each method with 1-8 non-out parameters, the source generator produces a wrapper struct:

```
{MethodName}_Setup (generated, readonly struct)
├── _inner: MethodSetupBuilder<TReturn>
├── [forwards all IMethodSetup<TReturn> methods]
├── Returns(Func<T1, T2, ..., TReturn>) → ISetupChain<TReturn>   [typed]
├── Callback(Action<T1, T2, ...>) → ISetupChain<TReturn>          [typed]
└── Throws(Func<T1, T2, ..., Exception>) → ISetupChain<TReturn>   [typed]
```

Same pattern for void methods: `{MethodName}_VoidSetup` wrapping `VoidMethodSetupBuilder`.
