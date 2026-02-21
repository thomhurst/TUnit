# Implementation Plan: TUnit.Mocks Advanced Features (Beyond Parity)

**Branch**: `001-mock-library` | **Date**: 2026-02-21 | **Spec**: `specs/001-mock-library/spec.md`
**Input**: Extend TUnit.Mocks beyond feature parity — strongly-typed callbacks, async verification, state machine mocking, diagnostics

## Summary

TUnit.Mocks has achieved full feature parity with Moq, NSubstitute, and FakeItEasy. This plan covers features that **go beyond** what any existing framework offers, leveraging TUnit.Mocks's source-generator architecture for capabilities that runtime-proxy frameworks cannot match.

Four core features:
1. **Strongly-typed callbacks/returns** — source-gen emits `Callback((int a, string b) => ...)` per method
2. **Async verification with TUnit assertions** — `await Assert.That(mock.Verify!.Method()).WasCalled(Times.Once)`
3. **State machine mocking** — `mock.InState("disconnected", s => s.Connect().TransitionsTo("connected"))`
4. **Mock diagnostics** — `mock.GetDiagnostics()` reports unused setups and unmatched calls

Plus planned (deferred) packages: `TUnit.Mocks.Logging`, `TUnit.Mocks.Http`.

## Technical Context

**Language/Version**: C# preview / .NET 10.0 (multi-target netstandard2.0;net8.0;net9.0;net10.0)
**Primary Dependencies**: Roslyn 4.14/4.7/4.4 (source generator), TUnit.Assertions (async verification only)
**Storage**: N/A
**Testing**: TUnit (test runner + assertions), custom snapshot infrastructure
**Target Platform**: Any .NET runtime (library)
**Project Type**: .NET class library + Roslyn source generator
**Performance Goals**: Zero allocations in mock dispatch hot path; typed callbacks add no overhead vs untyped
**Constraints**: AOT compatible, no System.Reflection.Emit, strong-named assembly, netstandard2.0 compat
**Scale/Scope**: Existing 8-project solution, ~362 tests, ~34 files modified

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|---|---|---|
| I. Performance First | PASS | Typed callbacks wrap to existing behaviors — no new allocations in hot path. State guard is one string comparison in FindMatchingSetup. |
| II. Intuitive & Easy-to-Use API | PASS | Typed callbacks improve DX. Async verification aligns with TUnit patterns. State machine API is fluent and declarative. |
| III. Flexibility & Extensibility | PASS | All features are additive — no breaking changes to existing user code except typed callback return type change (beta library). |
| IV. Dual-Mode Compatibility | N/A | TUnit.Mocks is source-gen only (not dual-mode like TUnit core engine). |
| V. AOT & Modern .NET | PASS | No dynamic/reflection added. All new code is AOT-compatible. |
| VI. Quality Gates | PASS | Snapshot tests updated for generator changes. All tests run via Microsoft.Testing.Platform. |

## Project Structure

### Documentation (this feature)

```text
specs/001-mock-library/
├── plan.md              # This file
├── research.md          # Research decisions (27 decisions + 4 new)
├── data-model.md        # Entity model for new features
├── quickstart.md        # Usage examples
├── contracts/           # Public API contracts
│   └── public-api.md
└── tasks.md             # Task breakdown (via /speckit.tasks)
```

### Source Code (repository root)

```text
TUnit.Mocks/                              # Runtime library (netstandard2.0;net8.0;net9.0;net10.0)
├── Setup/
│   ├── IMethodSetup.cs                  # MODIFY: add typed overload method signatures
│   ├── IVoidMethodSetup.cs              # MODIFY: same
│   ├── ISetupChain.cs                   # MODIFY: add TransitionsTo(string)
│   ├── MethodSetup.cs                   # MODIFY: add RequiredState, TransitionTarget
│   ├── MethodSetupBuilder.cs            # MODIFY: implement TransitionsTo, typed overloads
│   └── VoidMethodSetupBuilder.cs        # MODIFY: same
├── Verification/
│   ├── ICallVerification.cs             # MODIFY: add CheckWasCalled → AssertionResult
│   ├── CallVerificationBuilder.cs       # MODIFY: implement non-throwing checks
│   ├── IPropertyVerification.cs         # MODIFY: add Check* methods
│   └── PropertyVerificationBuilder.cs   # MODIFY: implement non-throwing checks
├── [NO assertion classes here — TUnit.Mocks has zero dependency on TUnit.Assertions]
├── Diagnostics/                         # NEW: mock diagnostics
│   ├── MockDiagnostics.cs               # NEW: diagnostics record type
│   ├── SetupInfo.cs                     # NEW: setup metadata for diagnostics
│   └── UnmatchedCallInfo.cs             # NEW
├── MockEngine.cs                        # MODIFY: state machine support, diagnostics
├── MockOfT.cs                           # MODIFY: SetState, InState, GetDiagnostics
└── TUnit.Mocks.csproj                    # NO dependency changes

TUnit.Mocks.Assertions/                   # NEW: Bridge package (TUnit.Mocks + TUnit.Assertions)
├── TUnit.Mocks.Assertions.csproj         # NEW: references TUnit.Mocks + TUnit.Assertions
├── WasCalledAssertion.cs                # NEW: Assertion<ICallVerification>
├── WasNeverCalledAssertion.cs           # NEW
├── MockAssertionExtensions.cs           # NEW: extension methods on IAssertionSource<ICallVerification>
└── PropertyAssertionExtensions.cs       # NEW: same for IPropertyVerification

TUnit.Mocks.SourceGenerator/
├── Builders/
│   ├── MockSetupBuilder.cs              # MODIFY: generate typed wrapper structs
│   └── MockVerifyBuilder.cs             # MODIFY: same pattern for typed verify
└── Models/
    └── MockMemberModel.cs               # READ: parameter types already available

TUnit.Mocks.Tests/                        # Tests
├── TypedCallbackTests.cs                # MODIFY: add strongly-typed tests
├── AsyncVerificationTests.cs            # NEW
├── StateMachineTests.cs                 # NEW
├── DiagnosticsTests.cs                  # NEW
└── ...existing test files...

TUnit.Mocks.SourceGenerator.Tests/
└── Snapshots/                           # MODIFY: update snapshots for typed wrapper structs
```

**Structure Decision**: All features go into the existing `TUnit.Mocks` and `TUnit.Mocks.SourceGenerator` projects. No new projects needed for core features. `TUnit.Mocks.Logging` and `TUnit.Mocks.Http` are future separate projects.

## Feature Design

### Feature 1: Strongly-Typed Callbacks and Returns

**Problem**: Current callbacks receive `object?[]` — users must cast manually.

**Solution**: Source generator emits per-method wrapper struct with typed overloads.

**Generated code example** (for `int Add(int a, int b)`):

```csharp
// Generated in {SafeName}_MockSetupExtensions
public static Add_Setup Add(this IMockSetup<ICalculator> setup, Arg<int> a, Arg<int> b)
{
    var s = (ICalculator_MockSetup)setup;
    var matchers = new IArgumentMatcher[] { a.Matcher, b.Matcher };
    var methodSetup = new MethodSetup(0, matchers, "Add");
    s.Engine.AddSetup(methodSetup);
    return new Add_Setup(new MethodSetupBuilder<int>(methodSetup));
}

// Generated wrapper struct
public readonly struct Add_Setup : IMethodSetup<int>
{
    private readonly MethodSetupBuilder<int> _inner;
    internal Add_Setup(MethodSetupBuilder<int> inner) => _inner = inner;

    // Forward all IMethodSetup<int> members
    public ISetupChain<int> Returns(int value) => _inner.Returns(value);
    public ISetupChain<int> Returns(Func<int> factory) => _inner.Returns(factory);
    // ... all existing methods forwarded ...

    // NEW: typed overloads
    public ISetupChain<int> Returns(Func<int, int, int> factory)
        => _inner.Returns(args => factory((int)args[0]!, (int)args[1]!));

    public ISetupChain<int> Callback(Action<int, int> callback)
        => _inner.Callback(args => callback((int)args[0]!, (int)args[1]!));

    public ISetupChain<int> Throws(Func<int, int, Exception> factory)
        => _inner.Throws(args => factory((int)args[0]!, (int)args[1]!));
}
```

**User API**:
```csharp
mock.Setup.Add(Arg.Any<int>(), Arg.Any<int>())
    .Returns((int a, int b) => a + b);       // Typed!

mock.Setup.Add(Arg.Any<int>(), Arg.Any<int>())
    .Callback((int a, int b) => log.Add(a)); // Typed!
```

**Scope**: Only methods with 1-8 non-out parameters get typed overloads. 0-param methods don't need them (existing `Action` callback works). >8 params fall back to `object?[]`.

### Feature 2: Async Verification with TUnit Assertions (Separate Package)

**Problem**: Mock verification throws synchronously, doesn't integrate with `Assert.That()` pipeline.

**Solution**: New `TUnit.Mocks.Assertions` NuGet package that bridges `TUnit.Mocks` and `TUnit.Assertions`. Zero changes to `TUnit.Mocks` itself — the bridge wraps existing sync verification.

**User API**:
```csharp
// NEW: async, integrates with Assert.Multiple() and AssertionScope
// Requires: dotnet add package TUnit.Mocks.Assertions
await Assert.That(mock.Verify!.Add(1, 2)).WasCalled(Times.Once);
await Assert.That(mock.Verify!.Reset()).WasNeverCalled();

// OLD: still works (sync, throws directly, no extra package needed)
mock.Verify!.Add(1, 2).WasCalled(Times.Once);
```

**Implementation** (all in `TUnit.Mocks.Assertions` package — TUnit.Mocks unchanged):
```csharp
// Bridge assertion class — wraps existing sync verification
public sealed class WasCalledAssertion : Assertion<ICallVerification>
{
    private readonly Times _times;
    protected override Task<AssertionResult> CheckAsync(...)
    {
        try
        {
            metadata.Value!.WasCalled(_times);
            return Task.FromResult(AssertionResult.Passed);
        }
        catch (MockVerificationException ex)
        {
            return Task.FromResult(AssertionResult.Failed(ex.Message));
        }
    }
    protected override string GetExpectation()
        => $"to have been called {_times}";
}

// Extension methods on IAssertionSource<ICallVerification>
public static class MockAssertionExtensions
{
    public static WasCalledAssertion WasCalled(
        this IAssertionSource<ICallVerification> source, Times times) => ...;
    public static WasNeverCalledAssertion WasNeverCalled(
        this IAssertionSource<ICallVerification> source) => ...;
}
```

**Package structure**:
- `TUnit.Mocks.Assertions.csproj` — references `TUnit.Mocks` + `TUnit.Assertions`
- `TUnit.Mocks` has **zero dependency** on `TUnit.Assertions`
- Users who use TUnit as their test framework add this package for seamless integration
- Users on xUnit/NUnit/MSTest use the sync API with no extra package

### Feature 3: State Machine Mocking

**Problem**: Testing stateful behavior (retry logic, connections, circuit breakers) requires mutable closures.

**Solution**: Named states as first-class guards on method setups.

**User API**:
```csharp
var mock = Mock.Of<IConnection>();
mock.SetState("disconnected");

mock.InState("disconnected", setup => {
    setup.Connect().TransitionsTo("connected");
    setup.GetStatus().Returns("OFFLINE");
});

mock.InState("connected", setup => {
    setup.Connect().Throws<InvalidOperationException>();
    setup.GetStatus().Returns("ONLINE");
    setup.Disconnect().TransitionsTo("disconnected");
});

IConnection conn = mock;
Assert.That(conn.GetStatus()).IsEqualTo("OFFLINE");
conn.Connect();                                        // → state: "connected"
Assert.That(conn.GetStatus()).IsEqualTo("ONLINE");
conn.Disconnect();                                     // → state: "disconnected"
```

**Implementation** (all in runtime library, no generator changes):
- `MockEngine._currentState: string?` — mutable state field
- `MethodSetup.RequiredState: string?` — guard condition (null = match in any state)
- `MethodSetup.TransitionTarget: string?` — new state after behavior executes
- `FindMatchingSetup`: skip setups where `RequiredState != _currentState`
- `HandleCall`/`HandleCallWithReturn`: after behavior, apply `TransitionTarget`
- `ISetupChain.TransitionsTo(string)` / `IVoidSetupChain.TransitionsTo(string)`
- `Mock<T>.SetState(string?)` / `Mock<T>.InState(string, Action<IMockSetup<T>>)`
- `InState` uses scoped `PendingRequiredState` on engine — `AddSetup` reads and stamps it

### Feature 4: Mock Diagnostics

**Problem**: No framework helps detect over-mocking, dead setups, or untested code paths.

**Solution**: `mock.GetDiagnostics()` returns a structured report.

**User API**:
```csharp
var diag = mock.GetDiagnostics();
// diag.UnusedSetups — setups never triggered (dead test code)
// diag.UnmatchedCalls — calls that matched no setup (potential missing coverage)
// diag.TotalSetups / diag.ExercisedSetups — coverage ratio
```

**Implementation**:
- `MethodSetup.InvokeCount` already tracked — setups with 0 are unused
- `MockEngine` tracks unmatched calls (calls that fell through to default behavior)
- New `bool _wasUnmatched` flag on `CallRecord`, set when no setup matched
- `MockDiagnostics` is a readonly record aggregating this data

## Complexity Tracking

No constitution violations. All features are additive to the existing architecture.

| Feature | New Files | Modified Files | Generator Changes |
|---|---|---|---|
| Typed callbacks | 0 | 2 (MockSetupBuilder.cs, MockVerifyBuilder.cs) | Yes — struct generation |
| Async verification | 5 (new project) | 0 (TUnit.Mocks unchanged) | No |
| State machine | 0 | 6 (MockEngine, MethodSetup, chains, MockOfT) | No |
| Diagnostics | 3 | 2 (MockEngine, MockOfT) | No |

## Implementation Order

1. **State machine mocking** (Feature 3) — no generator changes, self-contained in runtime library
2. **Mock diagnostics** (Feature 4) — no generator changes, purely additive
3. **Async verification** (Feature 2) — adds TUnit.Assertions dependency, new assertion classes
4. **Strongly-typed callbacks** (Feature 1) — generator changes, snapshot updates, most complex

All four features are independent of each other. The order above minimizes risk — runtime-only features first, new project second, generator changes last.

## Verification

1. `dotnet build TUnit.Mocks/TUnit.Mocks.csproj` — builds on all 4 TFMs, 0 warnings
2. `dotnet run --project TUnit.Mocks.Tests --framework net10.0` — all tests pass (362+)
3. `dotnet test --project TUnit.Mocks.Analyzers.Tests --framework net10.0` — 22 pass
4. `dotnet test --project TUnit.Mocks.SourceGenerator.Tests --framework net10.0` — snapshots match
5. `dotnet build TUnit.Pipeline/TUnit.Pipeline.csproj -c Release` — builds clean
