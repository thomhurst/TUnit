# Research: TUnit.Mocks Source-Generated Mocking Library

**Branch**: `001-mock-library` | **Date**: 2026-02-20

## Decision 1: Mock Discovery Mechanism

**Decision**: Auto-detect `Mock.Of<T>()` invocations using `CreateSyntaxProvider` in an `IIncrementalGenerator`.

**Rationale**: This provides the best developer ergonomics — no attributes or registration needed. The generator scans for `InvocationExpressionSyntax` matching `Mock.Of<T>()`, extracts the type argument via semantic model, and generates mock implementations for each unique `T`. This is the pattern used by EF Core interceptor generators and Skugga.

**Alternatives Considered**:
- `ForAttributeWithMetadataName` with `[GenerateMock<T>]` — 99x faster predicate (Roslyn pre-indexes attributes) but requires boilerplate declarations. Rejected for worse DX.
- `ForInvocationWithName` — proposed in Roslyn issue #77887 (not yet released as of Feb 2026). Would be the ideal fast-path but not available.

**Implementation Notes**:
- Phase 1 (syntax predicate): Pattern-match `InvocationExpressionSyntax` → `MemberAccessExpressionSyntax` → `GenericNameSyntax { Identifier.ValueText: "Of" }` + `IdentifierNameSyntax { Identifier.ValueText: "Mock" }`. Zero allocations, string comparison only.
- Phase 2 (semantic transform): Resolve `IMethodSymbol` via `GetSymbolInfo`, confirm `ContainingType` is `TUnit.Mocks.Mock`, extract `TypeArguments[0]` as `INamedTypeSymbol`.
- **Critical**: Never store `ISymbol` or `SyntaxNode` in the pipeline model. Extract into fully equatable value records (`MockTypeModel`). Use `EquatableArray<T>` wrapper for member lists.
- Deduplication: `.Collect().SelectMany(arr => arr.Distinct())` ensures each type generates one mock class even if referenced in many files.

## Decision 2: Generated Mock Architecture

**Decision**: Generate a nested mock implementation class inside a `Mock<T>` wrapper, using per-member handler lists for setup matching and `ConcurrentQueue<CallRecord>` for call recording.

**Rationale**: Follows the Rocks pattern (proven at scale) but adapted for TUnit.Mocks's API style (`.Setup` / `.Verify` / `.Raise` properties instead of Rocks' expectations model).

**Alternatives Considered**:
- Interceptors (C# 12) to replace `Mock.Of<T>()` calls at the call site — more complex, requires interceptor feature flag, limits to same-assembly mocking. Rejected as over-engineering for initial release.
- Single monolithic mock class without wrapper — rejected because we need both the mock object (implements `T`) and the control surface (`.Setup`, `.Verify`, `.Reset()`).

**Implementation Pattern**:

The source generator produces for each `Mock.Of<IFoo>()`:

1. **`IFoo_MockImpl`** — sealed class implementing `IFoo`. Each member routes through `MockEngine<IFoo>`:
   - Records call in `ConcurrentQueue<CallRecord>`
   - Iterates setups (last-added-first-matched) to find matching `ISetup`
   - Executes matched behavior (return, throw, callback, sequential chain)
   - Falls back to smart default (loose) or throws (strict)

2. **`IFoo_MockSetup`** — generated class mirroring `IFoo`'s members but returning `IMethodSetup<TReturn>` builders instead of `TReturn`. Accepts `Arg<T>` parameters (implicit conversion from `T`).

3. **`IFoo_MockVerify`** — generated class mirroring `IFoo`'s members but returning `ICallVerification` builders. Same `Arg<T>` parameter pattern.

4. **`IFoo_MockRaise`** — generated class with methods for each event on `IFoo`.

## Decision 3: Interface Member Discovery

**Decision**: Use `ITypeSymbol.GetMembers()` for direct members + `ITypeSymbol.AllInterfaces` for inherited interface members. Detect overlapping signatures across base interfaces for explicit implementation.

**Rationale**: `AllInterfaces` provides the flat transitive closure — no manual recursion needed. The critical complexity is detecting when the same logical signature appears in multiple base interfaces (e.g., `IEnumerable<T>` and `IEnumerable` both declaring `GetEnumerator()`), requiring explicit interface implementation for each.

**Key Patterns**:
- Methods with `out` params: Always initialize `out T param = default!;` at top of method body before handler logic.
- Generic methods with constraints: Emit full constraint clauses. Detect `where T : default` need for nullable-annotated type parameters.
- Default interface methods (DIMs): Generate shim objects that inherit the interface and forward to the mock, used as fallback when no handler matches.
- Indexers: Treated as properties with parameters — same handler pattern.
- Explicit interface implementation: No access modifier, prefix member name with `InterfaceName.`.

## Decision 4: Thread Safety Strategy

**Decision**: Setups stored in `List<ISetup>` protected by `ReaderWriterLockSlim` (read-heavy access pattern). Call history recorded in `ConcurrentQueue<CallRecord>` (lock-free append, batch read for verification).

**Rationale**: Setup is typically done during test arrange phase (single-threaded), but call recording happens during act phase (potentially concurrent). `ConcurrentQueue` provides zero-contention append. `ReaderWriterLockSlim` allows concurrent reads during call matching without blocking.

**Alternatives Considered**:
- `Interlocked.Increment` on call count only (Rocks pattern) — insufficient because we need full argument capture for verification messages.
- `ConcurrentStack<T>` for setups (NSubstitute pattern) — rejected because we need last-added-first-matched iteration order, which `ConcurrentStack` provides via LIFO but `List` with reverse iteration is simpler.

## Decision 5: Nullability-Aware Smart Defaults

**Decision**: Inspect `ITypeSymbol.NullableAnnotation` at source-generation time to determine default return values. Non-nullable reference types get sensible defaults; nullable types get `null`.

**Rationale**: The source generator has access to full nullability annotation information. Generating smart defaults at compile time means zero runtime reflection and perfect type safety.

**Default Value Matrix**:

| Return Type | NullableAnnotation | Default Value |
|-------------|-------------------|---------------|
| `string` | NotAnnotated | `""` |
| `string?` | Annotated | `null` |
| `IList<T>` | NotAnnotated | `new List<T>()` |
| `IList<T>?` | Annotated | `null` |
| `IEnumerable<T>` | NotAnnotated | `Array.Empty<T>()` |
| `Task` | NotAnnotated | `Task.CompletedTask` |
| `Task<T>` | NotAnnotated | `Task.FromResult(default(T))` |
| `ValueTask` | N/A (value type) | `default` |
| `ValueTask<T>` | N/A (value type) | `new ValueTask<T>(default(T))` |
| Any value type | N/A | `default(T)` |
| Any other ref type | NotAnnotated | `default!` (suppress warning) |
| Any other ref type | Annotated | `null` |

## Decision 6: Roslyn Multi-Version Strategy

**Decision**: Follow TUnit's established pattern with three Roslyn variant projects (Roslyn44, Roslyn47, Roslyn414) sharing source from the base project via `Roslyn.props`.

**Rationale**: Exact reuse of existing infrastructure. `Directory.Build.props` auto-derives `_BaseProjectName`, `RoslynVersion`, `AssemblyName`, and `RootNamespace` from the project name pattern via regex. `Roslyn.props` globs in all `.cs` files from the base project and overrides `Microsoft.CodeAnalysis` package versions.

**Key Details**:
- Variant csproj files are one-liners: `<Import Project="..\Roslyn.props" />`
- All variants produce the same assembly name (`TUnit.Mocks.SourceGenerator`)
- NuGet package slots: `analyzers/dotnet/roslyn4.4/cs/`, `analyzers/dotnet/roslyn4.7/cs/`, `analyzers/dotnet/roslyn4.14/cs/`
- Compile constants: `ROSLYN4_7_OR_GREATER`, `ROSLYN4_14_OR_GREATER` for version-conditional code

## Decision 7: NuGet Packaging Strategy

**Decision**: Single `TUnit.Mocks` NuGet package that bundles the runtime library, analyzers, and all three Roslyn-versioned source generators. Follow `TUnit.Core` packaging pattern (not `TUnit.Assertions` which uses a single generator slot).

**Rationale**: Roslyn-versioned slots ensure compatibility across VS 2022 versions, Rider, and `dotnet build` with different SDK versions. The three-slot approach is the TUnit standard for source generators.

**Package Layout**:
```
lib/
  netstandard2.0/TUnit.Mocks.dll
  net8.0/TUnit.Mocks.dll
  net9.0/TUnit.Mocks.dll
  net10.0/TUnit.Mocks.dll
analyzers/dotnet/cs/
  TUnit.Mocks.Analyzers.dll
analyzers/dotnet/roslyn4.4/cs/
  TUnit.Mocks.SourceGenerator.dll
analyzers/dotnet/roslyn4.7/cs/
  TUnit.Mocks.SourceGenerator.dll
analyzers/dotnet/roslyn4.14/cs/
  TUnit.Mocks.SourceGenerator.dll
build/netstandard2.0/
  TUnit.Mocks.props
  TUnit.Mocks.targets
buildTransitive/{tfm}/
  TUnit.Mocks.props
  TUnit.Mocks.targets
```

## Decision 8: Verification Failure Mechanism

**Decision**: Throw `MockVerificationException` (inherits `Exception`) on verification failure. Throw `MockStrictBehaviorException` for unconfigured calls in strict mode.

**Rationale**: Exception-based failures are universally caught by all test frameworks (TUnit, xUnit, NUnit, MSTest) without requiring framework-specific integration. Keeps TUnit.Mocks truly standalone.

## Decision 9: Sequential Behavior Chain Implementation

**Decision**: Implement chained behaviors as a `List<IBehavior>` with an index that advances on each call. The last behavior repeats for all calls beyond the chain length.

**Rationale**: Simple, predictable semantics. The `.Then()` method appends to the behavior list. Each call increments the index. When index exceeds list length, the last behavior is reused. Thread-safe via `Interlocked.Increment` on the index.

---

## Feature Parity Decisions (2026-02-21)

## Decision 10: Verification Tracking for VerifyNoOtherCalls

**Decision**: Add a `bool IsVerified` flag to `CallRecord` and a `MarkVerified()` method. `CallVerificationBuilder.WasCalled()` marks matched calls as verified. `VerifyNoOtherCalls()` filters call history for unverified entries.

**Rationale**: Simple flag-based approach. No structural changes to call recording. Thread-safe via the existing lock patterns.

**Alternatives Considered**:
- Separate verified call set in a HashSet — more memory, more complexity for cross-referencing. Rejected.
- Hash-based tracking — over-engineering for typical test scenarios with <100 calls per mock.

## Decision 11: VerifyAll Implementation

**Decision**: Track which `MethodSetup` instances have been invoked at least once via an `InvokeCount` counter on MethodSetup. `VerifyAll()` iterates all registered setups and throws `MockVerificationException` listing any setup with `InvokeCount == 0`.

**Rationale**: `MethodSetup` already tracks call index for sequential behaviors. Adding an invocation counter is trivial and has zero overhead in the call hot path (just an increment).

## Decision 12: Multiple Interface Mocking

**Decision**: Support `Mock.Of<T1, T2>()` with up to 4 type parameters. The source generator creates a single impl class implementing all interfaces. The primary type parameter `T1` is used for `Mock<T1>` return type. Setup/Verify extension methods include members from all interfaces.

**Rationale**: 4 is sufficient — Moq caps at 4 via `.As<T>()`. Beyond 4 type parameters, users should refactor their design.

**Key Challenge**: Member name collisions across interfaces. Resolved by using explicit interface implementation in the generated impl class. Setup/verify extension methods are disambiguated by fully-qualified prefix when names collide.

## Decision 13: Mock Repository

**Decision**: Simple `MockRepository` class with internal `List<IMock>` tracking. `Of<T>()` creates and tracks mocks. Batch operations (VerifyAll, VerifyNoOtherCalls, Reset) iterate all tracked mocks, aggregating failures into a single exception.

**Rationale**: Keep it simple. The repository is a convenience wrapper, not a complex orchestration mechanism. Aggregated exceptions prevent early termination masking additional failures.

## Decision 14: Auto-Track Properties

**Decision**: Add `Dictionary<int, object?>` to MockEngine for property auto-tracking. When `SetupAllProperties()` is called, a flag enables auto-tracking. Generated property setters store values by member ID; getters check the dictionary before falling back to explicit setup/default. Explicit setups always take precedence.

**Rationale**: Dictionary lookup by int key is O(1) amortized. Memory overhead is negligible for typical mock property counts (<20 properties per interface).

## Decision 15: Custom Delegate Events

**Decision**: Extend source generator event handling to inspect the event's delegate type. For non-EventHandler delegates (e.g., `Action<string, int>`, custom delegates), generate: a backing field of the delegate type, add/remove handlers, and a raise method whose parameters match the delegate's Invoke method signature.

**Rationale**: Standard `EventHandler`/`EventHandler<T>` events already work. Extending to arbitrary delegates requires the generator to decompose the delegate's Invoke method and generate matching raise parameters.

## Decision 16: Recursive/Auto Mocking

**Decision**: When an unconfigured method returns a mockable type (interface or abstract class), lazily create and cache a mock instance. The source generator detects these return types at generation time and emits auto-mock return logic. A transitive discovery pass ensures mock implementations exist for all returned mockable types.

**Rationale**: Matches Moq's `DefaultValue.Mock` behavior. Lazy creation avoids unnecessary allocations. Caching per member ID ensures identity stability (same mock returned on repeated calls).

**Key Findings (2026-02-21)**:
- `MockMemberModel.ReturnType` stores FQN as string — pipeline-safe
- `MockTypeDiscovery.TransformToModels()` can yield additional models for transitive types
- `MockEngine` caches auto-mocks in `ConcurrentDictionary<string, object>` keyed by return type FQN
- Recursion depth limited to 3 to prevent infinite loops on circular references (A→B→A)
- Auto-mocking only in Loose mode; Strict mode throws as usual
- Users retrieve auto-mock wrappers via `mock.GetAutoMock<TChild>()`

**Risk**: Circular references (IFoo returns IBar, IBar returns IFoo). Mitigated by lazy initialization — the mock is only created when the method is actually called, not during mock construction.

## Decision 17: Delegate Mocking

**Decision**: `Mock.OfDelegate<TDelegate>()` creates a `MockDelegate<T>` wrapping a delegate instance. The source generator inspects the delegate's Invoke method signature and generates setup/verify extension methods.

**Rationale**: Delegates are essentially single-method interfaces. The same setup/verify pattern works with minimal generator adjustments — just inspect `INamedTypeSymbol.DelegateInvokeMethod` instead of enumerating interface members.

**Key Findings (2026-02-21)**:
- `INamedTypeSymbol.DelegateInvokeMethod` provides full Invoke signature
- `MemberDiscovery.CreateMethodModel()` can process the Invoke method as a `MockMemberModel`
- Delegate mock has exactly one mockable member — simpler than interface mocks
- `MockDelegate<T>` holds engine, setup, verify; exposes `.Delegate` and implicit conversion to `T`
- Separate `_delegateFactories` registry needed since delegates have different constraints

## Decision 22: Typed Callbacks — Method-Specific Interface Pattern

**Decision**: Use source-generated method-specific interfaces that inherit from `IMethodSetup<TReturn>` to enable typed overloads without ambiguity.

**Rationale**: Plain extension methods on `IMethodSetup<TReturn>` would collide when multiple methods share the same return type. A method-specific interface (e.g., `IGetGreeting_Setup : IMethodSetup<string>`) scopes the typed overloads to the correct method. All existing `.Returns(value)` / `.Throws()` / `.Callback(Action)` methods still work because the interface inherits from `IMethodSetup<TReturn>`.

**Alternatives considered**:
- Wrapper class per-method: Too many allocations, breaks fluent chaining
- Only `object?[]` overloads: Works but loses the ergonomic advantage that competing frameworks have

## Decision 23: Out/Ref Value Assignment — Engine-Level Side Channel

**Decision**: Add an `outArgs` array parameter to `HandleCallWithReturn`. The generated mock impl creates this array, passes it through, and reads back assigned values after engine dispatch.

**Rationale**: Out parameter values must flow back to the generated implementation code. The engine already has the behavior execution result; adding a parallel output array for out/ref params is the simplest mechanism. The `MethodSetup` stores assignment functions alongside the behavior chain.

**Alternatives considered**:
- Return a tuple `(TReturn result, object?[] outValues)`: Breaks the existing `IBehavior` contract
- Only support out values via typed callbacks: Too limited — users want `.Returns(true).SetsOut("value", "found")`

## Decision 24: Negation Matcher — Wrapper Pattern

**Decision**: `NotMatcher<T>` wraps any `IArgumentMatcher<T>` and inverts its result.

**Rationale**: Composable. Users can negate any existing matcher: `Arg.Not(Arg.Is<int>(x => x > 0))`. Follows FakeItEasy's `That.Not.Matches(...)` pattern but as a standalone combinator.

## Decision 25: Custom Default Value Provider

**Decision**: Simple `IDefaultValueProvider` interface with `CanProvide(Type)` and `GetDefaultValue(Type)`. Injected into `MockEngine` at construction time.

**Rationale**: Matches Moq's `DefaultValueProvider` pattern. Checked after explicit setups but before auto-mock logic.

## Decision 26: Computed Throws

**Decision**: Add `Throws(Func<Exception>)` and `Throws(Func<object?[], Exception>)` overloads, plus source-generated typed `Throws(Func<T1, ..., Exception>)`.

**Rationale**: Consistent with the `Returns(Func<TReturn>)` pattern. Users can throw different exceptions based on arguments.

## Decision 27: Strongly-Typed Callbacks and Returns (Source-Gen)

**Decision**: Generate per-method wrapper structs that wrap `IMethodSetup<TReturn>` / `IVoidMethodSetup` and add typed `Callback` / `Returns` / `Throws` overloads. The source generator emits inline casts through existing `ComputedReturnWithArgsBehavior<T>` and `CallbackWithArgsBehavior`.

**Rationale**: This is something **only a source-generator framework can do**. Moq/NSubstitute/FakeItEasy require `object[]` args or expression trees. The source generator already knows exact parameter types via `MockParameterModel.FullyQualifiedType` and can emit `Callback((int a, string b) => ...)` and `Returns((int a, string b) => a + b.Length)` per method.

**Pattern**: Per-method named struct wrapping `IMethodSetup<TReturn>`. Forward all existing methods, add typed overloads that internally wrap into `object?[]` dispatch.

**Breaking Change**: Setup extension methods return struct instead of `IMethodSetup<TReturn>`. Fluent chaining (`mock.Setup.Method().Returns(...)`) still works. Storing result in `IMethodSetup<T>` variable will need updating. Acceptable for beta library.

**Alternatives Considered**:
- Extension methods on `IMethodSetup<TReturn>`: Leaks overloads across methods with same return type — rejected
- New `IBehavior<T1,T2>` per arity: Unnecessary complexity — rejected
- Expression trees (Moq pattern): AOT-incompatible — rejected

**Key Files**:
- `MockSetupBuilder.cs` — generate wrapper struct per method, change return type
- `MockVerifyBuilder.cs` — same pattern for typed verify accessors
- No changes to `IBehavior` interface or existing behavior classes

## Decision 28: Async Verification with TUnit Assertions (Separate Package)

**Decision**: Create `TUnit.Mocks.Assertions` bridge package. `TUnit.Mocks` itself has **zero dependency** on `TUnit.Assertions`. The bridge wraps existing sync verification in `Assertion<T>` subclasses via try/catch.

**Rationale**: TUnit.Mocks must remain standalone (usable with xUnit/NUnit/MSTest). The assertion integration is opt-in via a separate package that references both `TUnit.Mocks` and `TUnit.Assertions`. This follows the same principle as `TUnit.Mocks.Logging` — no dependency pollution in the base package.

**Implementation** (all in `TUnit.Mocks.Assertions` package):
1. `WasCalledAssertion : Assertion<ICallVerification>` — wraps `WasCalled(Times)` in try/catch, converts `MockVerificationException` to `AssertionResult.Failed`
2. Extension methods on `IAssertionSource<ICallVerification>`: `.WasCalled(Times)`, `.WasNeverCalled()`
3. Same pattern for `IPropertyVerification`
4. **Zero changes to TUnit.Mocks** — the bridge uses existing public APIs only

**Key Design**: The bridge catches `MockVerificationException` and converts to `AssertionResult.Failed(ex.Message)`. This avoids any coupling between `TUnit.Mocks` and `TUnit.Assertions` types.

**Alternatives Considered**:
- Add `AssertionResult` return methods to `ICallVerification`: Creates dependency on TUnit.Assertions — rejected (violates standalone requirement)
- `[GenerateAssertion]` on static methods: Name conflicts with sync overloads — rejected
- Make `WasCalled()` return `Task`: Breaks sync API, no `AssertionScope` — rejected

## Decision 29: Mock Diagnostics / Unused Setup Detection

**Decision**: Add `mock.GetDiagnostics()` returning `MockDiagnostics` record with unused setups, unmatched calls, and setup coverage metrics.

**Rationale**: No existing framework provides this. `MethodSetup.InvokeCount` already tracks usage — setups with count 0 are "unused". `MockEngine` tracks all calls and which were verified. Purely additive read-only API.

**Key Design**:
```csharp
public record MockDiagnostics(
    IReadOnlyList<SetupInfo> UnusedSetups,
    IReadOnlyList<CallRecord> UnmatchedCalls,  // calls that hit no setup (loose mode default returns)
    int TotalSetups,
    int ExercisedSetups
);
```

## Decision 30: State Machine Mocking

**Decision**: Add `RequiredState` guard on `MethodSetup` and `TransitionTarget` post-match effect. No generator changes needed.

**Rationale**: TUnit.Mocks would be **the first major .NET mocking library with named state machine support**. Inspired by WireMock.NET's `InScenario().WhenStateIs().WillSetStateTo()`.

**Implementation**:
- `MockEngine._currentState: string?` — checked in `FindMatchingSetup` before `Matches(args)`
- `MethodSetup.RequiredState: string?` — if non-null, must match engine state
- `MethodSetup.TransitionTarget: string?` — applied after behavior executes
- `ISetupChain.TransitionsTo(string)` — sets transition target
- `Mock<T>.SetState(string?)` / `Mock<T>.InState(string, Action<IMockSetup<T>>)` — API surface

**Alternatives Considered**:
- `TransitionBehavior : IBehavior`: Wrong abstraction — `IBehavior.Execute` has no engine reference — rejected
- Thread-local pending state: Not thread-safe for parallel setup — rejected

## Decision 31: Common Mock Helpers (Separate Packages)

**Decision**: Create dedicated NuGet packages for domain-specific mock helpers:
- `TUnit.Mocks.Logging` — ILogger<T> verification helpers (depends on `Microsoft.Extensions.Logging.Abstractions`)
- `TUnit.Mocks.Http` — HttpMessageHandler/HttpClient mocking (depends on `System.Net.Http`)

**Rationale**: `TUnit.Mocks` base package MUST NOT pull in framework-specific dependencies. ILogger verification is the #1 mocking pain point; HttpClient is #2. Each helper package depends on `TUnit.Mocks` + the relevant framework package.

**Deferred**: Lower priority than core features (Decisions 27-30). Plan now, implement in future phase.

## Decision 18: Protected Member Mocking

**Decision**: The generated impl class overrides protected members and routes through MockEngine identically to public members. Protected members appear directly on the Setup/Verify surfaces.

**Rationale**: Source generation has full access to protected members. No string-based reflection needed (unlike Moq's `mock.Protected().Setup<int>("MethodName", ...)`).

## Decision 19: Wrap Real Object

**Decision**: `Mock.Wrap<T>(instance)` creates a mock that delegates un-configured calls to the real instance. The generated impl stores a `_wrappedInstance` field and calls `_wrappedInstance.Method()` for non-configured calls instead of `base.Method()`.

**Rationale**: Differs from partial mocking only in dispatch target — wrapped instance vs base class. Same generator infrastructure can be extended.

**Key Findings (2026-02-21)**:
- Partial mock impl already generates `base.Method()` calls — extend to `_wrappedInstance.Method()`
- Only classes with virtual methods can be wrapped (same constraint as partial mocks)
- All calls (both delegated and overridden) recorded through MockEngine for verification
- Wrapping null or sealed types should produce clear errors

## Decision 20: Auto-Raise Event on Method Call

**Decision**: Add `.Raises(eventName, args)` to `MethodSetup` return type. After executing the behavior, engine checks for raise info and invokes the event.

**Rationale**: Runtime-only feature. `MethodSetup` already stores behaviors — event raise info is an additional behavior that executes after the primary behavior.

## Decision 21: Event Subscription Setup

**Decision**: Add callback hooks to `MockEngine.RecordEventSubscription()`. `OnSubscribe(eventName, callback)` and `OnUnsubscribe(eventName, callback)` on `Mock<T>`.

**Rationale**: The engine already tracks subscriptions via `RecordEventSubscription()`. Adding callback invocation is minimal — just a dictionary lookup and invoke.
