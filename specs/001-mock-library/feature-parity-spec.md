# Feature Specification: TUnit.Mocks Feature Parity & Beyond

**Feature Branch**: `001-mock-library`
**Created**: 2026-02-21
**Status**: Draft
**Input**: Close all feature parity gaps against Moq, NSubstitute, and FakeItEasy. TUnit.Mocks should match and exceed other frameworks while keeping an easy and intuitive API.

## Context

TUnit.Mocks is a source-generated, AOT-compatible mocking framework. It already leads in AOT support, thread safety, implicit arg capture, and compile-time analyzers. This specification addresses the remaining feature gaps identified in the mocking framework comparison matrix where TUnit.Mocks has "No" and at least one competitor has "Yes."

### Current Gaps (from comparison matrix)

| Gap | Moq | NSubstitute | FakeItEasy | Impact |
|-----|:---:|:-----------:|:----------:|--------|
| Recursive/auto mocking | Yes | Yes | Yes | High — all three competitors have it |
| Wrap real object | Yes | Yes | Yes | Medium — useful for legacy code testing |
| Delegate mocking | No | Yes | Yes | Medium — two competitors have it |
| Auto-raise event on method call | Yes | No | No | Low — only Moq has it |
| Event subscription setup | Yes | No | Yes | Low — niche use case |
| LINQ to Mocks | Yes | No | No | Low — Moq-only, niche syntax sugar |

## User Scenarios & Testing

### User Story 1 - Recursive/Auto Mocking (Priority: P1)

A developer mocks an interface whose method returns another interface. Without configuring the nested return, they expect a usable mock object (not null) so they can chain calls without boilerplate setup.

**Why this priority**: All three competing frameworks support this. It is the most commonly requested missing feature because real-world dependency graphs are deeply nested. Without auto-mocking, users must manually create and wire up mocks for every level of the dependency chain.

**Independent Test**: Can be fully tested by creating a mock of an interface with a method returning another interface, calling that method without setup, and verifying the returned object is a functional mock that can itself be configured and verified.

**Acceptance Scenarios**:

1. **Given** a mock of `IService` where `IService.GetRepository()` returns `IRepository`, **When** the user calls `mock.Object.GetRepository()` without setup, **Then** a functional mock of `IRepository` is returned (not null)
2. **Given** an auto-mocked `IRepository` returned from scenario 1, **When** the user sets up a method on the auto-mocked child, **Then** the setup works identically to a manually created mock
3. **Given** an auto-mocked `IRepository`, **When** the user verifies calls on it, **Then** verification works identically to a manually created mock
4. **Given** a mock in strict mode, **When** a method returning an interface is called without setup, **Then** auto-mocking still applies (strict mode controls unexpected calls, not return values)
5. **Given** a deeply nested chain (`IService → IRepository → ILogger`), **When** the user accesses `mock.Object.GetRepository().GetLogger()`, **Then** each level auto-generates a mock that is reusable across calls (same instance returned each time)

---

### User Story 2 - Delegate Mocking (Priority: P2)

A developer needs to mock a delegate type (`Func<T>`, `Action<T>`, or a custom delegate) to inject as a dependency. They should be able to set up return values, throw exceptions, capture arguments, and verify calls — all with the same API as interface mocking.

**Why this priority**: Two competing frameworks (NSubstitute, FakeItEasy) support delegate mocking. Delegates are commonly used as lightweight dependencies in modern .NET (e.g., factory functions, event handlers, strategy patterns).

**Independent Test**: Can be fully tested by creating a delegate mock, configuring its return value, invoking it, and verifying the invocation.

**Acceptance Scenarios**:

1. **Given** a developer creates a mock of `Func<string, int>`, **When** they set it up to return `42` for any input, **Then** invoking the delegate returns `42`
2. **Given** a mock of `Action<string>`, **When** the developer invokes it with `"hello"`, **Then** the call can be verified
3. **Given** a mock of a custom delegate `delegate int Calculator(int a, int b)`, **When** setup and invoked, **Then** it works identically to built-in delegate types
4. **Given** a delegate mock with `Arg.Any<string>()` capture, **When** invoked multiple times, **Then** all argument values are captured
5. **Given** a delegate mock, **When** the developer uses `.Throws<Exception>()`, **Then** the delegate throws on invocation

---

### User Story 3 - Wrap Real Object (Priority: P3)

A developer has an existing object instance and wants to wrap it in a mock so that most method calls delegate to the real implementation, but specific methods can be overridden with mock behavior. This is useful for testing legacy code or complex objects where only a subset of behavior needs to be replaced.

**Why this priority**: All three competing frameworks support some form of wrapping. It enables incremental testing of legacy systems where full mocking is impractical.

**Independent Test**: Can be fully tested by creating a real service object, wrapping it in a mock, overriding one method, and verifying that non-overridden methods call the real implementation while the overridden method uses the mock behavior.

**Acceptance Scenarios**:

1. **Given** a real `ConcreteService` instance, **When** the developer wraps it with `Mock.Wrap(realInstance)`, **Then** all virtual method calls delegate to the real instance by default
2. **Given** a wrapped mock, **When** the developer sets up a specific method, **Then** that method uses the mock behavior instead of the real implementation
3. **Given** a wrapped mock, **When** a non-overridden method is called, **Then** the call is still recorded for verification purposes
4. **Given** a wrapped mock, **When** the real object's method has side effects, **Then** those side effects occur for non-overridden calls
5. **Given** a sealed class or non-virtual method, **When** the developer attempts to wrap it, **Then** a clear compile-time or runtime error indicates the limitation

---

### User Story 4 - Auto-Raise Event on Method Call (Priority: P4)

A developer sets up a method on a mock and chains `.Raises(e => e.SomeEvent, args)` so that when the method is called, the specified event is automatically raised. This simplifies testing event-driven workflows.

**Why this priority**: Only Moq supports this. It is a convenience feature that reduces setup boilerplate for event-driven code, but developers can achieve the same result with callbacks.

**Independent Test**: Can be fully tested by setting up a method with `.Raises()`, subscribing to the event, calling the method, and verifying the event handler was invoked.

**Acceptance Scenarios**:

1. **Given** a mock of `IService` with event `StatusChanged`, **When** the developer sets up `mock.Setup.Process(Arg.Any<int>()).Returns(true).Raises(m => m.StatusChanged, EventArgs.Empty)`, **Then** calling `Process()` returns `true` AND raises `StatusChanged`
2. **Given** multiple events chained with `.Raises()`, **When** the method is called, **Then** all specified events fire in order
3. **Given** `.Raises()` with custom event args, **When** the event fires, **Then** subscribers receive the correct event arguments

---

### User Story 5 - Event Subscription Setup (Priority: P5)

A developer wants to configure behavior that triggers when an event is subscribed to or unsubscribed from. For example, a mock could invoke a callback when a consumer first subscribes to an event, enabling testing of lazy-initialization patterns.

**Why this priority**: Only Moq and FakeItEasy support this. It is a niche feature for testing event-subscription side effects, which are uncommon in typical application code.

**Independent Test**: Can be fully tested by setting up behavior on event subscription, subscribing an event handler, and verifying the configured behavior was triggered.

**Acceptance Scenarios**:

1. **Given** a mock with `OnSubscribe(m => m.DataReady, callback)`, **When** a handler subscribes to `DataReady`, **Then** the callback executes
2. **Given** a mock with `OnUnsubscribe(m => m.DataReady, callback)`, **When** a handler unsubscribes from `DataReady`, **Then** the callback executes

---

### Edge Cases

- What happens when auto-mocking encounters a circular dependency (A returns B returns A)?
- What happens when a delegate mock is invoked without any setup in strict mode?
- What happens when wrapping an object whose constructor has side effects?
- What happens when auto-raising an event that has no subscribers?
- What happens when wrapping a null instance?

## Requirements

### Functional Requirements

- **FR-001**: The library MUST auto-generate mock instances for interface return types when no setup is configured (recursive/auto mocking)
- **FR-002**: Auto-mocked child objects MUST return the same instance on repeated calls to the same property or method
- **FR-003**: Auto-mocked child objects MUST support full setup, verification, and argument capture
- **FR-004**: The library MUST support mocking of `Func<>` and `Action<>` delegate types with arbitrary arity
- **FR-005**: The library MUST support mocking of custom delegate types with the same capabilities as interface mocking
- **FR-006**: Delegate mocks MUST support return value setup, exception throwing, callbacks, argument capture, and call verification
- **FR-007**: The library MUST support wrapping an existing object instance so that un-configured virtual methods delegate to the real implementation
- **FR-008**: Wrapped objects MUST record all method calls (both delegated and overridden) for verification
- **FR-009**: The library MUST support auto-raising events when a configured method is called (`.Raises()` chaining)
- **FR-010**: The library MUST support configuring callbacks on event subscription and unsubscription
- **FR-011**: All new features MUST work with the existing AOT-compatible, source-generated architecture
- **FR-012**: All new features MUST maintain the existing fluent API style and be discoverable through the existing `mock.Setup` / `mock.Verify` pattern
- **FR-013**: Auto-mocking MUST handle circular references gracefully (return the same auto-mock without infinite recursion)
- **FR-014**: Wrapping MUST produce clear errors when applied to sealed types or non-virtual methods

### Key Entities

- **Auto-Mock**: A mock automatically generated when a mocked method returns an interface type without explicit setup
- **Delegate Mock**: A mock wrapping a delegate type, supporting setup, invocation, and verification
- **Wrapped Mock**: A mock that delegates un-configured calls to a real object instance
- **Event Raise Chain**: A setup extension that triggers an event when the configured method is called

## Success Criteria

### Measurable Outcomes

- **SC-001**: The feature matrix comparison page shows zero "No" entries for TUnit.Mocks in any row where at least one competitor has "Yes" (excluding sealed classes and static methods, which no framework supports)
- **SC-002**: All new features have corresponding test coverage with at least 5 tests per feature area
- **SC-003**: Existing test suite continues to pass with no regressions (currently 380 tests across 3 test projects)
- **SC-004**: All new features work under Native AOT compilation without warnings
- **SC-005**: Developer experience requires no more than 2 lines of code difference compared to equivalent operations in Moq, NSubstitute, or FakeItEasy
- **SC-006**: New features integrate with existing `mock.Setup` / `mock.Verify` API patterns — no new top-level entry points required (except `Mock.Wrap()` for object wrapping)

## Assumptions

- LINQ to Mocks (`Mock.Of<T>(x => x.Prop == value)`) is intentionally excluded from scope. It is a Moq-only syntax sugar feature that conflicts with TUnit.Mocks's extension-method-based design. The same result is achievable with standard setup calls.
- Sealed class and static method mocking are excluded — no competing framework supports these either.
- Auto-mocking applies to interface return types only. Concrete class return types will return smart defaults (not auto-mocked instances) to avoid unexpected constructor side effects.
- Delegate mocking uses a dedicated factory method (e.g., `Mock.OfDelegate<Func<string, int>>()`) since delegates are not interfaces or classes with virtual members.

## Out of Scope

- LINQ to Mocks expression syntax
- Sealed class mocking
- Static method mocking
- Non-virtual method interception on concrete classes
- Deep-copy support for captured arguments
