# Feature Specification: TUnit.Mocks — Source-Generated Mocking Library

**Feature Branch**: `001-mock-library`
**Created**: 2026-02-20
**Status**: Draft
**Input**: User description: "Create a TUnit mocking library (TUnit.Mocks). Standalone project independent of TUnit core, compatible with any test framework. Modern, flexible, intuitive, powerful, performant, easy to use. Leverage source generators, interceptors, and modern C# features. Feature parity with NSubstitute and Moq, with ambition to go further."

## Clarifications

### Session 2026-02-20

- Q: How should verification failures surface to test runners across frameworks? → A: Throw a dedicated exception type (e.g., `MockVerificationException`) on failure — universal framework compatibility.
- Q: How should async methods (Task<T>, ValueTask<T>, IAsyncEnumerable<T>) be supported? → A: First-class support with unified `.Returns()` overloads — the source generator knows the method's return type at compile time and automatically wraps values in Task.FromResult/ValueTask/etc. No separate `.ReturnsAsync()` needed. Same unification applies to `.Throws()` (returns faulted task for async methods).
- Q: Can mocks be reset mid-test, and how? → A: Single `.Reset()` that clears all setups and call history at once. No granular reset — keeps the API simple and avoids confusing partial-state bugs.
- Q: Should sequential setup support chained behaviors (not just values)? → A: Yes. Sequential chaining MUST support mixed behaviors per call (e.g., `.Throws().Then().Returns().Then().Callback()`), not just different return values. The last chained behavior repeats for all subsequent calls. This is critical for testing retry logic, circuit breakers, and state transitions.
- Q: What should unconfigured methods return in loose mode for collection/reference types? → A: Nullability-aware smart defaults. Non-nullable reference types get sensible defaults (empty collections, `""` for string, completed tasks). Nullable reference types (`T?`) return `null`. Value types return `default(T)`. The source generator inspects nullability annotations to determine the correct behavior at compile time.
- Q: How does the source generator discover which types to generate mocks for? → A: Auto-detect from usage. The generator scans for `Mock.Of<T>()` calls in user code and generates mock implementations for each unique `T` found. No attributes or explicit registration needed.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Create a Mock and Configure Return Values (Priority: P1)

A developer writing a unit test needs to create a mock of an interface or abstract class and configure methods to return specific values. They install the TUnit.Mocks NuGet package, annotate their type (or use a simple API call), and the source generator produces a concrete mock implementation at compile time. They configure return values using a fluent, strongly-typed API — no lambda expression trees, no `.Object` wrapper. IntelliSense guides them through the entire process.

**Why this priority**: This is the foundational use case for any mocking library. Without creating mocks and configuring returns, nothing else works. It must feel effortless to go from zero to a working mock.

**Independent Test**: Can be fully tested by creating a mock of a simple interface with one method, configuring a return value, calling the method, and verifying the returned value matches.

**Acceptance Scenarios**:

1. **Given** a developer has an interface `ICalculator` with a method `int Add(int a, int b)`, **When** they create a mock and configure `Add(2, 3)` to return `5`, **Then** calling `Add(2, 3)` on the mock returns `5`.
2. **Given** a developer creates a mock of an interface, **When** they call a method that has no explicit setup, **Then** the method returns the default value for its return type (loose mode behavior).
3. **Given** a developer creates a mock of an abstract class with virtual methods, **When** they configure a virtual method's return value, **Then** the mock returns the configured value while non-virtual methods call the base implementation.
4. **Given** a developer writes a mock setup with incorrect types, **When** they compile, **Then** they get a compile-time error (not a runtime exception).

---

### User Story 2 — Verify Method Calls Were Made (Priority: P1)

A developer needs to verify that their system-under-test called specific methods on its dependencies with expected arguments and expected call counts. They use a strongly-typed verification API that provides clear, descriptive failure messages when expectations are not met.

**Why this priority**: Verification is the second fundamental pillar of mocking (alongside stubbing). Tests that only stub but never verify cannot catch interaction bugs. This must be equally intuitive as configuring returns.

**Independent Test**: Can be tested by creating a mock, calling methods on it, then verifying call counts and arguments through the verification API.

**Acceptance Scenarios**:

1. **Given** a mock of `IEmailSender` with method `Send(string to, string body)`, **When** the system-under-test calls `Send("alice@example.com", "Hello")`, **Then** verification that `Send` was called once with those exact arguments succeeds.
2. **Given** a mock where `Send` was called 3 times, **When** the developer verifies it was called exactly once, **Then** verification fails with a clear message showing the actual call count and the arguments of each call.
3. **Given** a mock, **When** the developer verifies a method was never called, **Then** verification succeeds if the method was indeed never called, and fails with details if it was.

---

### User Story 3 — Use Argument Matchers (Priority: P1)

A developer needs flexible matching when configuring returns or verifying calls — matching any value, matching by predicate, matching by type, or capturing arguments for later inspection. The matcher API is consistent across setup and verification.

**Why this priority**: Real-world tests rarely match exact argument values. Argument matchers are essential for writing practical, maintainable tests. They also enable argument capture, which simplifies complex verification scenarios.

**Independent Test**: Can be tested by configuring a mock with various matchers (any, predicate, type) and verifying that calls match or don't match as expected.

**Acceptance Scenarios**:

1. **Given** a mock configured with `Arg.Any<string>()` for a parameter, **When** the method is called with any string value, **Then** the configured return value is returned.
2. **Given** a mock configured with `Arg.Is<int>(x => x > 0)` for a parameter, **When** the method is called with `5`, **Then** it matches; **When** called with `-1`, **Then** it does not match and returns the default.
3. **Given** a developer uses `Arg.Capture<string>()` to capture arguments, **When** the method is called multiple times, **Then** all captured values are available for assertion after the calls.

---

### User Story 4 — Mock Methods with Callbacks and Side Effects (Priority: P2)

A developer needs a mocked method to execute custom logic when called — logging, modifying captured state, throwing exceptions conditionally, or invoking callback arguments. They also need sequential return values (return different values on successive calls).

**Why this priority**: Callbacks and sequential returns handle the ~30% of mocking scenarios that simple return-value configuration cannot. They are essential for testing retry logic, state machines, and complex interaction patterns.

**Independent Test**: Can be tested by configuring callbacks and sequential returns, then verifying the side effects and return value progression.

**Acceptance Scenarios**:

1. **Given** a mock configured with a callback on method `Process(string item)`, **When** `Process` is called, **Then** the callback executes with the actual arguments.
2. **Given** a mock configured with sequential returns `[1, 2, 3]` for method `GetNext()`, **When** `GetNext()` is called three times, **Then** it returns `1`, `2`, `3` in order. The last value (`3`) repeats for any further calls.
3. **Given** a mock configured to throw `InvalidOperationException` on method `Validate()`, **When** `Validate()` is called, **Then** the configured exception is thrown.
4. **Given** a mock configured with chained behaviors `.Throws<TimeoutException>().Then().Returns(true)` on method `Connect()`, **When** `Connect()` is called twice, **Then** the first call throws `TimeoutException` and the second call returns `true`.

---

### User Story 5 — Mock Properties and Events (Priority: P2)

A developer needs to mock properties (get/set) and events (subscribe/raise). Properties can be configured to return values or track assignments. Events can be raised programmatically to test event handler logic in the system-under-test.

**Why this priority**: Properties and events are common in .NET interfaces. Without support for these, developers would need to create manual test doubles for any interface that includes them, defeating the purpose of the library.

**Independent Test**: Can be tested by configuring property getters/setters and raising events, then verifying the behavior.

**Acceptance Scenarios**:

1. **Given** an interface with property `string Name { get; set; }`, **When** the developer configures the getter to return `"Test"`, **Then** reading `Name` returns `"Test"`.
2. **Given** a mock with a settable property, **When** the system-under-test sets the property to `"Updated"`, **Then** the developer can verify the property was set and inspect the value.
3. **Given** a mock with event `EventHandler<string> OnMessage`, **When** the developer raises the event with `"hello"`, **Then** any subscriber attached by the system-under-test receives the event with `"hello"`.

---

### User Story 6 — Strict Mode and Automatic Verification (Priority: P2)

A developer wants to ensure that their system-under-test makes no unexpected calls to dependencies. They create a mock in strict mode, where any unconfigured call throws an exception immediately. Optionally, they can enable automatic verification at test teardown to catch unverified calls without explicit verify statements.

**Why this priority**: Strict mode is essential for certain testing philosophies (e.g., London school TDD). Automatic verification reduces boilerplate and catches subtle bugs where expected interactions never happened.

**Independent Test**: Can be tested by creating strict mocks, making unexpected calls, and verifying that exceptions are thrown.

**Acceptance Scenarios**:

1. **Given** a mock created in strict mode, **When** an unconfigured method is called, **Then** an exception is thrown immediately with a message listing the unexpected call and its arguments.
2. **Given** a mock created in loose mode (default), **When** an unconfigured method is called, **Then** the default value for the return type is returned without error.
3. **Given** automatic verification is enabled, **When** a configured expectation (e.g., `Method` should be called once) is never satisfied by end of test, **Then** the test fails with a descriptive message.

---

### User Story 7 — Handle out/ref Parameters and Generic Methods (Priority: P2)

A developer needs to mock methods that use `out` parameters, `ref` parameters, or generic type parameters. The API handles these naturally without special workarounds or awkward delegate syntax.

**Why this priority**: `out`/`ref` parameters are common in .NET APIs (e.g., `TryParse`, `TryGetValue`). Generic methods appear frequently in repository/service patterns. Poor support for these is one of the top complaints about existing mocking frameworks.

**Independent Test**: Can be tested by mocking methods with `out`/`ref` parameters and generic type parameters, configuring them, and verifying behavior.

**Acceptance Scenarios**:

1. **Given** an interface with `bool TryGet(string key, out string value)`, **When** the developer configures the mock to return `true` with `value = "found"` for key `"mykey"`, **Then** calling `TryGet("mykey", out var v)` returns `true` and sets `v` to `"found"`.
2. **Given** an interface with `ref int Increment(ref int value)`, **When** the mock is configured, **Then** the ref parameter is correctly modified per the configuration.
3. **Given** an interface with `T Get<T>(string key)`, **When** the developer configures `Get<int>("count")` to return `42`, **Then** calling `Get<int>("count")` returns `42`.

---

### User Story 8 — Partial Mocks and Base Class Call-Through (Priority: P3)

A developer needs to mock only some methods of a concrete or abstract class while letting other methods execute their real implementation. This is the "spy" or "partial mock" pattern, useful for testing classes with complex inherited behavior.

**Why this priority**: While interfaces are the primary mocking target, many real-world codebases have abstract base classes or concrete classes with virtual methods. Partial mocking avoids the need to wrap everything in interfaces solely for testability.

**Independent Test**: Can be tested by creating a partial mock of an abstract class, overriding one method while calling through to the base for another.

**Acceptance Scenarios**:

1. **Given** an abstract class `DataProcessor` with virtual method `Transform()` and concrete method `Process()` that calls `Transform()`, **When** the developer creates a partial mock and overrides only `Transform()`, **Then** calling `Process()` executes the real `Process()` logic, which calls the mocked `Transform()`.
2. **Given** a partial mock, **When** no setup is provided for a virtual method, **Then** the base class implementation is called (not the default value).

---

### User Story 9 — Ordered Call Verification (Priority: P3)

A developer needs to verify that calls to one or more mocks happened in a specific order. This is important for protocol testing, state machine validation, and ensuring correct sequencing of operations.

**Why this priority**: While most tests don't need ordered verification, protocol tests and integration scenarios depend on it. This is a differentiator over Moq (which lacks it) and matches NSubstitute/FakeItEasy capabilities.

**Independent Test**: Can be tested by making calls in a specific order and verifying the sequence passes, then making calls out of order and verifying the sequence check fails.

**Acceptance Scenarios**:

1. **Given** two mocks `IAuth` and `ILogger`, **When** the system-under-test calls `auth.Login()` then `logger.Log("logged in")`, **Then** ordered verification confirms this sequence.
2. **Given** the same mocks, **When** the calls happen in reverse order, **Then** ordered verification fails with a message showing the expected vs. actual order.

---

### User Story 10 — Compile-Time Safety and Diagnostics (Priority: P1)

A developer benefits from compile-time errors and analyzer warnings when they misuse the mocking API — attempting to mock a sealed class, mocking a struct, using incorrect argument types in setup, or forgetting to await an async mock setup. The source generator and companion analyzers catch these issues before the code ever runs.

**Why this priority**: Compile-time safety is a fundamental differentiator of source-generated mocking over runtime-proxy mocking. Catching errors at compile time saves significant debugging time and is a primary reason to choose this library.

**Independent Test**: Can be tested by writing code that violates mocking rules and confirming that compiler errors/warnings are produced with clear, actionable messages.

**Acceptance Scenarios**:

1. **Given** a developer attempts to mock a sealed class, **When** they compile, **Then** a compiler error is emitted: "TM001: Cannot mock sealed type 'Foo'. Consider extracting an interface."
2. **Given** a developer attempts to mock a struct, **When** they compile, **Then** a compiler error is emitted: "TM002: Cannot mock value type 'Bar'. Mocking requires reference types."
3. **Given** a developer sets up a method with wrong argument types, **When** they compile, **Then** the standard C# type-checking catches the mismatch (because the generated API is strongly typed).

---

### User Story 11 — Advanced Verification: VerifyNoOtherCalls and VerifyAll (Priority: P1)

A developer wants to ensure their system-under-test makes no unexpected calls beyond what was explicitly verified, or confirm that all configured setups were actually exercised. `VerifyNoOtherCalls()` catches unexpected interactions; `VerifyAll()` catches unused setups that indicate dead code or incomplete test coverage. Call history is also exposed publicly for custom inspection.

**Why this priority**: These are table-stakes verification features present in Moq. They represent significant competitive gaps — teams migrating from Moq expect these capabilities.

**Independent Test**: Create mock with setups, call some methods, verify that `VerifyNoOtherCalls` fails when unexpected calls exist, and `VerifyAll` fails when setups go uninvoked.

**Acceptance Scenarios**:

1. **Given** a mock where `Method1` and `Method2` were called, **When** only `Method1` is verified and `VerifyNoOtherCalls()` is called, **Then** verification fails listing the unverified `Method2` call with its arguments.
2. **Given** a mock where all calls have been verified, **When** `VerifyNoOtherCalls()` is called, **Then** verification succeeds.
3. **Given** a mock with setups for `Method1` and `Method2`, **When** only `Method1` was called, **Then** `VerifyAll()` fails listing the uninvoked `Method2` setup.
4. **Given** a mock with all setups invoked at least once, **When** `VerifyAll()` is called, **Then** verification succeeds.
5. **Given** a mock, **When** the developer accesses `mock.Invocations`, **Then** they get a read-only list of all calls made with method names, arguments, and timestamps.

---

### User Story 12 — Advanced Argument Matchers: Regex, Collections, and Custom Types (Priority: P1)

A developer needs richer argument matching beyond any/predicate/capture — matching strings by regex pattern, matching collections by content, and defining reusable custom matchers. These matchers work uniformly in both setup and verification.

**Why this priority**: Regex and collection matchers are among the most-requested features. Custom matcher types enable extensibility without library changes. Together they close the argument matching gap against all three competitor frameworks.

**Independent Test**: Configure setup with regex matcher on string param, verify it matches/rejects. Use collection matchers. Create custom matcher, use it in setup.

**Acceptance Scenarios**:

1. **Given** a mock configured with a regex matcher for a string parameter, **When** the method is called with a matching string, **Then** the setup matches; **When** called with a non-matching string, **Then** it does not match.
2. **Given** a mock configured with a collection "contains" matcher, **When** the method is called with a collection containing the expected item, **Then** the setup matches.
3. **Given** a developer creates a custom matcher class implementing the matcher interface, **When** they use it in setup or verification, **Then** it is applied correctly.
4. **Given** a collection matcher for "has count N", **When** the method is called with a collection of size N, **Then** it matches; with a different size, **Then** it does not match.

---

### User Story 13 — Multiple Interface Mocking (Priority: P2)

A developer needs a single mock that implements multiple interfaces simultaneously, useful when a dependency is expected to implement several interfaces (e.g., `IDisposable` and a domain interface). The mock can be set up and verified for members from any of the implemented interfaces.

**Why this priority**: Multiple interface mocking is supported by Moq, NSubstitute, and FakeItEasy. It handles a common real-world pattern where dependencies implement multiple contracts.

**Independent Test**: Create mock implementing `IFoo` and `IDisposable`, configure methods from both, verify calls on both.

**Acceptance Scenarios**:

1. **Given** a developer requests a mock implementing `IFoo` and `IBar`, **When** they cast the mock to either interface, **Then** it can be used as both.
2. **Given** a multi-interface mock, **When** the developer sets up methods from `IFoo` and verifies calls on `IBar`, **Then** both work correctly.
3. **Given** a multi-interface mock, **When** the interfaces have no overlapping members, **Then** all members from all interfaces are available.

---

### User Story 14 — Mock Repository and Batch Operations (Priority: P2)

A developer managing many mocks in a complex test wants to create, verify, and reset all mocks through a central repository. The repository tracks all mocks created through it and supports batch `VerifyAll()`, `VerifyNoOtherCalls()`, and `Reset()`.

**Why this priority**: Mock repositories reduce boilerplate in integration-style tests with many dependencies. This is a Moq feature that simplifies complex test setups.

**Independent Test**: Create repository, create multiple mocks through it, call batch verify, confirm it checks all mocks.

**Acceptance Scenarios**:

1. **Given** a repository with 3 mocks, **When** `repository.VerifyAll()` is called, **Then** all setups across all 3 mocks are verified.
2. **Given** a repository with mocks, **When** `repository.Reset()` is called, **Then** all mocks are reset to initial state.
3. **Given** a repository, **When** a mock created through it has unverified calls, **Then** `repository.VerifyNoOtherCalls()` fails listing the unverified calls.

---

### User Story 15 — Auto-Track Properties (Priority: P2)

A developer wants all properties on a mock to automatically behave like real properties — storing values on set and returning the last set value on get — without configuring each property individually. This is useful for mocking configuration objects or DTOs.

**Why this priority**: Moq's `SetupAllProperties()` and NSubstitute's automatic property tracking are frequently used convenience features that avoid tedious per-property configuration.

**Independent Test**: Enable auto-track, set a property, read it back, confirm the set value is returned.

**Acceptance Scenarios**:

1. **Given** a mock with auto-track enabled, **When** a property is set to `"Hello"` then read, **Then** it returns `"Hello"`.
2. **Given** a mock with auto-track enabled and a specific property also configured via setup, **When** the property is read, **Then** the explicit setup takes precedence over auto-tracking.
3. **Given** a mock with auto-track enabled, **When** a property has not been set, **Then** it returns the smart default for its type.

---

### User Story 16 — Advanced Event Support (Priority: P2)

A developer needs to mock events that use custom delegate types (not just `EventHandler<T>`), verify that event handlers were subscribed, and configure events to auto-raise when specific methods are called.

**Why this priority**: Events with custom delegates are common in WPF, Blazor, and signal-based architectures. Auto-raise and subscription verification close gaps against Moq and NSubstitute.

**Independent Test**: Define interface with custom delegate event, mock it, raise the event, verify subscriber received it.

**Acceptance Scenarios**:

1. **Given** an interface with an event using a custom delegate type (e.g., `Action<string, int>`), **When** the developer raises the event, **Then** subscribers receive the event with the correct arguments.
2. **Given** a mock configured to auto-raise an event when a method is called, **When** the method is called, **Then** the event is raised automatically.
3. **Given** a mock with an event, **When** the system-under-test subscribes a handler, **Then** the developer can verify that the subscription occurred.

---

### User Story 17 — Protected Member Mocking (Priority: P3)

A developer needs to explicitly set up and verify protected virtual members of abstract/concrete classes. While partial mocks already override protected members, an explicit API is needed to configure return values and verify calls on protected methods.

**Why this priority**: Moq provides an explicit API for protected member mocking. While niche, some legacy codebases rely heavily on protected virtual methods in base classes.

**Independent Test**: Create partial mock, configure a protected virtual method's return value via explicit API, verify it returns the configured value.

**Acceptance Scenarios**:

1. **Given** an abstract class with a protected virtual method `GetSecret()`, **When** the developer configures it to return `"configured"` via the protected member API, **Then** calling the method through a public method that delegates to it returns `"configured"`.
2. **Given** a partial mock with protected members, **When** the developer verifies a protected method was called, **Then** verification succeeds if it was called.

---

### User Story 18 — Delegate Mocking (Priority: P3)

A developer needs to mock delegate types directly (e.g., `Func<string, int>`, `Action<string>`), creating a mock delegate that can be configured with return values and verified for invocations. This is useful when APIs accept delegates as parameters.

**Why this priority**: NSubstitute and FakeItEasy support delegate mocking. While niche, it eliminates manual lambda boilerplate in tests that deal with delegate-heavy APIs.

**Independent Test**: Create mock of `Func<string, int>`, configure to return 42 for any string, invoke it, verify call.

**Acceptance Scenarios**:

1. **Given** a mock of `Func<string, int>`, **When** configured to return `42` for any input and invoked with `"hello"`, **Then** it returns `42`.
2. **Given** a mock delegate, **When** invoked multiple times, **Then** the developer can verify the invocation count and captured arguments.

---

### User Story 19 — Recursive/Auto Mocking (Priority: P3)

A developer mocks an interface where methods return other interfaces. Instead of manually creating nested mocks, the library automatically creates mock instances for return types that are themselves mockable interfaces. This simplifies setup for deep object graphs.

**Why this priority**: Recursive mocking is supported by Moq, NSubstitute, and FakeItEasy. It significantly reduces boilerplate for tests involving deeply nested dependency chains. However, it requires careful source generator work and is the most complex feature in this set.

**Independent Test**: Create mock of `IFoo` where `GetBar()` returns `IBar`, access `mock.Object.GetBar()` without setup, verify it returns a non-null auto-mock, configure the auto-mock's methods.

**Acceptance Scenarios**:

1. **Given** an interface `IFoo` with method `IBar GetBar()`, **When** a mock of `IFoo` is created and `GetBar()` is called without setup, **Then** it returns an auto-generated mock of `IBar` (not null).
2. **Given** an auto-mocked return value, **When** the developer accesses methods on the returned mock, **Then** those methods also return smart defaults or auto-mocks.
3. **Given** an auto-mocked return value, **When** the developer explicitly configures `GetBar()` to return a specific mock, **Then** the explicit setup takes precedence over auto-mocking.

---

### Edge Cases

- What happens when a mocked interface inherits from multiple other interfaces with overlapping method signatures?
- How does the library handle mocking of `IDisposable` / `IAsyncDisposable` — does `Dispose()` behave specially or is it treated as any other method?
- What happens when a mocked method is configured multiple times with different argument patterns — which setup wins? (Answer: last-configured-first-matched, consistent with Moq/NSubstitute behavior.)
- How does the library handle recursive/self-referencing mocks (e.g., `IBuilder` where methods return `IBuilder`)?
- What happens when the source generator encounters an interface defined in a referenced assembly (not in the current compilation) — can it still generate a mock?
- How does the library handle interfaces with default interface methods (C# 8+)?
- What happens when mocking a generic interface with constraints (e.g., `IRepository<T> where T : class, new()`)?
- How are indexer properties (`this[int index]`) handled?
- What happens when a developer configures a mock in one thread and uses it from another — is the mock thread-safe?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Library MUST generate mock implementations for interfaces at compile time via source generators, producing concrete classes that implement all interface members.
- **FR-002**: Library MUST generate mock implementations for abstract classes at compile time, overriding all abstract and virtual members.
- **FR-003**: Library MUST provide a fluent, strongly-typed API for configuring return values on mocked methods, where all type mismatches are caught at compile time.
- **FR-004**: Library MUST provide a strongly-typed API for verifying that methods were called with expected arguments and expected call counts (never, once, exactly N, at least N, at most N, between N and M).
- **FR-005**: Library MUST support argument matchers including: any-value, predicate-based, type-based, and argument capture.
- **FR-006**: Library MUST support both loose mode (default) and strict mode (unconfigured calls throw exceptions). In loose mode, unconfigured calls MUST return nullability-aware smart defaults: non-nullable reference types return sensible values (empty collections for `IEnumerable<T>`/`IList<T>`/etc., `""` for `string`, completed tasks for `Task`/`ValueTask`), nullable reference types (`T?`) return `null`, and value types return `default(T)`.
- **FR-007**: Library MUST support configuring sequential behaviors via chaining (e.g., `.Throws().Then().Returns().Then().Callback()`), where each link in the chain defines the behavior for the Nth call. Behaviors can be any mix of return values, exceptions, and callbacks. The last chained behavior repeats for all subsequent calls beyond the chain length.
- **FR-008**: Library MUST support configuring callbacks/side effects that execute when a mocked method is called.
- **FR-009**: Library MUST support configuring mocked methods to throw specific exceptions.
- **FR-010**: Library MUST support mocking properties (get and set) with configuration and verification.
- **FR-011**: Library MUST support mocking events, including programmatic event raising.
- **FR-012**: Library MUST support methods with `out` and `ref` parameters without requiring special delegate workarounds.
- **FR-013**: Library MUST support mocking generic interfaces and generic methods.
- **FR-014**: Library MUST support partial mocks (spy pattern) for abstract and concrete classes with virtual members, where unconfigured virtual methods call through to the base implementation.
- **FR-015**: Library MUST support ordered call verification across one or more mocks.
- **FR-016**: Library MUST provide Roslyn analyzers that emit compile-time errors when developers attempt to mock sealed classes or value types.
- **FR-017**: Library MUST be fully compatible with Native AOT — no use of `System.Reflection.Emit` or runtime proxy generation.
- **FR-018**: Library MUST be a standalone NuGet package with no dependency on TUnit.Core, TUnit.Engine, or any specific test framework.
- **FR-019**: Library MUST produce clear, descriptive failure messages on verification failures, including the expected call, actual calls made, and argument values. Failures MUST surface by throwing a dedicated exception type (e.g., `MockVerificationException`) to ensure universal test framework compatibility.
- **FR-019a**: Strict mode violations (unconfigured calls) MUST throw a dedicated exception type (e.g., `MockStrictBehaviorException`) distinct from verification failures, so developers can distinguish between "unexpected call" and "expected call not made".
- **FR-020**: Library MUST support mocking interfaces defined in external referenced assemblies, not only those in the current compilation. The source generator MUST auto-discover types to mock by scanning for `Mock.Of<T>()` calls in user code — no attributes or explicit registration required.
- **FR-021**: Library MUST be thread-safe — mocks created in one context must be safely usable from multiple threads.
- **FR-022**: Library MUST support mocking interfaces with default interface method implementations.
- **FR-023**: Library MUST support mocking interfaces with indexer properties.
- **FR-024**: Library MUST support custom argument matchers that developers can create by implementing a matcher interface.
- **FR-025**: Library MUST provide a `.Reset()` method on mocks that clears all setups and call history at once, returning the mock to its initial state (loose or strict per original creation).
- **FR-026**: Library MUST provide first-class support for async methods returning `Task`, `Task<T>`, `ValueTask`, `ValueTask<T>`, and `IAsyncEnumerable<T>`. The setup API (`.Returns()`, `.Throws()`) MUST use unified overloads that automatically handle async wrapping — the developer passes the unwrapped value and the source generator handles `Task.FromResult`/`ValueTask`/faulted task creation transparently. No separate `.ReturnsAsync()` or `.ThrowsAsync()` methods are needed.
- **FR-027**: Library MUST provide `VerifyNoOtherCalls()` on mocks that fails when any recorded calls have not been matched by a prior verification statement.
- **FR-028**: Library MUST provide `VerifyAll()` on mocks that fails when any configured setup was never invoked.
- **FR-029**: Library MUST expose call history publicly via an `Invocations` property returning a read-only collection of call records including method name, arguments, and timestamp.
- **FR-030**: Library MUST provide a regex argument matcher for string parameters that matches based on a regular expression pattern.
- **FR-031**: Library MUST provide collection argument matchers including: contains item, has count, is empty, sequence equals, and predicate-over-elements.
- **FR-032**: Library MUST support user-defined custom argument matchers via a public interface that users can implement and pass to the matching API.
- **FR-033**: Library MUST support mocking a single object that implements multiple interfaces simultaneously, with setup and verification available for members from all interfaces.
- **FR-034**: Library MUST provide a `MockRepository` class for batch creation, verification, and reset of multiple mocks.
- **FR-035**: Library MUST support auto-tracking properties where setting a property stores the value and getting returns the last stored value, without explicit per-property configuration.
- **FR-036**: Library MUST support events with custom delegate types beyond `EventHandler` and `EventHandler<T>`, including `Action<T>`, `Func<T>`, and user-defined delegate types.
- **FR-037**: Library MUST support configuring events to auto-raise when a specific method is called on the mock.
- **FR-038**: Library MUST support verifying that an event handler was subscribed to a mocked event.
- **FR-039**: Library MUST provide an explicit API for setting up and verifying protected virtual members of mocked classes.
- **FR-040**: Library MUST support mocking delegate types directly (e.g., `Func<T, TResult>`, `Action<T>`), with setup and verification capabilities.
- **FR-041**: Library MUST support recursive/auto mocking where unconfigured methods returning mockable interface types automatically return generated mock instances.

### Key Entities

- **Mock**: The central object representing a mock instance. Wraps a generated implementation of the target type and provides access to setup and verification APIs. A mock operates in either loose or strict mode.
- **Setup**: A configuration applied to a specific method/property on a mock, defining what the mock should do when that member is called (return a value, execute a callback, throw an exception, return sequential values).
- **Argument Matcher**: A rule that determines whether an actual argument value matches an expected pattern. Built-in matchers include any-value, predicate-based, type-based, and capture. Developers can create custom matchers.
- **Verification**: An assertion that a specific method/property was called on a mock with expected arguments and call count. Produces descriptive failure messages on mismatch.
- **Call Record**: An internal record of each call made to a mocked member, including method identity, argument values, and timestamp. Used for verification and failure message generation.
- **Mock Behavior**: The mode governing how a mock handles unconfigured calls — loose (return defaults) or strict (throw exceptions).
- **Mock Repository**: A container that tracks multiple mocks and provides batch operations (VerifyAll, VerifyNoOtherCalls, Reset) across all tracked mocks.
- **Custom Matcher**: A user-defined argument matcher implementing a public interface, enabling domain-specific matching logic reusable across tests.

## Assumptions

- The library will use C# source generators (IIncrementalGenerator) as the primary code generation mechanism, since they are the most mature and well-supported approach for compile-time code generation in the .NET ecosystem.
- The library will target `netstandard2.0` (for broad compatibility) plus `net8.0`, `net9.0`, and `net10.0` (for modern features and AOT), consistent with TUnit.Assertions' multi-targeting strategy.
- The NuGet package will bundle the source generator and analyzers in the `analyzers/dotnet/cs` path, following the established TUnit.Assertions packaging pattern.
- The default mock behavior will be "loose" (return defaults for unconfigured calls), which is the industry standard default.
- Setup precedence will follow "last-configured-first-matched" semantics: when multiple setups could match a call, the most recently configured matching setup wins. This is consistent with Moq and NSubstitute behavior.
- The library will not attempt to mock static methods, sealed classes, or non-virtual members of concrete classes. These limitations will be clearly communicated via analyzer diagnostics rather than runtime errors.
- The mock instance will be the object itself (like NSubstitute), not a wrapper requiring `.Object` (like Moq).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A developer with no prior experience with TUnit.Mocks can create their first working mock, configure a return value, and verify a call within 5 minutes of reading the getting-started documentation.
- **SC-002**: Mock creation and method invocation must be at least 10x faster than equivalent operations using Castle DynamicProxy-based frameworks (Moq, NSubstitute), as measured by standardized benchmarks.
- **SC-003**: The library must produce zero runtime reflection for mock creation, call interception, and verification — enabling full Native AOT compatibility.
- **SC-004**: 100% of type mismatches in mock setup and verification are caught at compile time, not at runtime.
- **SC-005**: All features present in NSubstitute (return values, argument matchers, callbacks, sequential returns, event raising, received calls verification, ordered verification, partial mocks) have equivalent or superior functionality in TUnit.Mocks.
- **SC-005a**: TUnit.Mocks matches or exceeds Moq in verification capabilities — VerifyAll, VerifyNoOtherCalls, and mock repository batch operations are all supported.
- **SC-005b**: TUnit.Mocks matches or exceeds FakeItEasy in argument matching — regex, collection matchers, and custom matcher types are all supported.
- **SC-006**: Verification failure messages clearly identify: (a) what was expected, (b) what actually happened, (c) the argument values for each actual call — enabling developers to diagnose failures without stepping through a debugger.
- **SC-007**: The library works correctly with TUnit, xUnit, NUnit, and MSTest — verified by integration tests with each framework.
- **SC-008**: The library passes all Native AOT trimming analyzers with zero warnings when consumed in an AOT-published application.
- **SC-009**: Feature comparison table shows TUnit.Mocks as "Yes" for every feature where at least two of the three competitors (Moq, NSubstitute, FakeItEasy) also say "Yes".
