# Tasks: TUnit.Mocks Feature Parity & Beyond

**Input**: Design documents from `/specs/001-mock-library/`
**Prerequisites**: plan.md, feature-parity-spec.md, research.md
**Base**: 380 passing tests, existing TUnit.Mocks library fully functional

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1-US5)
- Include exact file paths in descriptions

---

## Phase 1: Foundational (Blocking Prerequisites)

**Purpose**: Runtime infrastructure changes needed before any user story

- [ ] T001 Add `IRaisable` interface with `RaiseEvent(string eventName, object? args)` method in TUnit.Mocks/IRaisable.cs
- [ ] T002 Make generated mock impl classes implement `IRaisable` by updating TUnit.Mocks.SourceGenerator/Builders/MockImplBuilder.cs to generate a `RaiseEvent` method that invokes the matching backing event field

**Checkpoint**: Generated impls can raise events programmatically via the engine

---

## Phase 2: US5 — Event Subscription Setup (Priority: P5)

**Goal**: Allow configuring callbacks when events are subscribed to or unsubscribed from

**Independent Test**: Create a mock with event subscription callbacks, subscribe/unsubscribe handlers, verify callbacks fire

### Implementation

- [ ] T003 [US5] Add `ConcurrentDictionary<string, Action> _onSubscribeCallbacks` and `_onUnsubscribeCallbacks` to MockEngine in TUnit.Mocks/MockEngine.cs
- [ ] T004 [US5] Add `OnSubscribe(string eventName, Action callback)` and `OnUnsubscribe(string eventName, Action callback)` to MockEngine in TUnit.Mocks/MockEngine.cs
- [ ] T005 [US5] Invoke callbacks in existing `RecordEventSubscription()` method in TUnit.Mocks/MockEngine.cs
- [ ] T006 [US5] Expose `OnSubscribe(string eventName, Action callback)` and `OnUnsubscribe(string eventName, Action callback)` on Mock<T> in TUnit.Mocks/MockOfT.cs
- [ ] T007 [US5] Create tests for event subscription callbacks in TUnit.Mocks.Tests/EventSubscriptionSetupTests.cs — test subscribe/unsubscribe callbacks fire, multiple subscriptions, unsubscribe callback, no callback when none configured
- [ ] T008 [US5] Update comparison matrix — change "Event subscription setup" from No to Yes in docs/docs/comparison/mocking-frameworks.md

**Checkpoint**: Event subscription setup works and is tested

---

## Phase 3: US4 — Auto-Raise Event on Method Call (Priority: P4)

**Goal**: Allow `.Raises(eventName, args)` on setup chain to auto-raise events when method is called

**Independent Test**: Set up a method with `.Raises()`, subscribe to event, call method, verify event fires

### Implementation

- [ ] T009 [US4] Add `EventRaiseInfo` record (EventName, Args) and `List<EventRaiseInfo> _eventRaises` storage to MethodSetup in TUnit.Mocks/Setup/MethodSetup.cs
- [ ] T010 [US4] Add `Raises(string eventName, EventArgs args)` fluent method to setup builder return types in TUnit.Mocks/Setup/MethodSetupBuilder.cs (both void and return variants)
- [ ] T011 [US4] Add `IRaisable? Raisable` property to MockEngine and pass it during construction in TUnit.Mocks/MockEngine.cs
- [ ] T012 [US4] After behavior execution in `HandleCall` and `HandleCallWithReturn`, check for event raise info and invoke via `IRaisable.RaiseEvent()` in TUnit.Mocks/MockEngine.cs
- [ ] T013 [US4] Update generated factory code to pass impl (as IRaisable) to MockEngine in TUnit.Mocks.SourceGenerator/Builders/MockFactoryBuilder.cs
- [ ] T014 [US4] Create tests for auto-raise event in TUnit.Mocks.Tests/AutoRaiseEventTests.cs — test single raise, multiple raises, raise with custom EventArgs, raise on void method, raise on return method
- [ ] T015 [US4] Update comparison matrix — change "Auto-raise on method call" from No to Yes in docs/docs/comparison/mocking-frameworks.md

**Checkpoint**: Auto-raise events work end-to-end

---

## Phase 4: US2 — Delegate Mocking (Priority: P2)

**Goal**: Support `Mock.OfDelegate<Func<string, int>>()` with full setup/verify capabilities

**Independent Test**: Create a delegate mock, configure return value, invoke, verify call

### Source Generator Changes

- [ ] T016 [US2] Add `IsDelegateType` flag to MockTypeModel in TUnit.Mocks.SourceGenerator/Models/MockTypeModel.cs
- [ ] T017 [US2] Add `IsMockOfDelegateInvocation()` syntax predicate to MockTypeDiscovery in TUnit.Mocks.SourceGenerator/Discovery/MockTypeDiscovery.cs — detect `Mock.OfDelegate<T>()` calls
- [ ] T018 [US2] Add `TransformDelegateToModel()` in MockTypeDiscovery that extracts `DelegateInvokeMethod` via `INamedTypeSymbol.DelegateInvokeMethod` and creates MockTypeModel with single method in TUnit.Mocks.SourceGenerator/Discovery/MockTypeDiscovery.cs
- [ ] T019 [US2] Add delegate pipeline to MockGenerator — register new syntax provider for delegate invocations, route to delegate-specific generation in TUnit.Mocks.SourceGenerator/MockGenerator.cs

### Runtime Changes

- [ ] T020 [US2] Create `MockDelegate<T>` class with Delegate property, Setup, Verify surfaces, implicit conversion to T in TUnit.Mocks/MockDelegate.cs
- [ ] T021 [US2] Add `_delegateFactories` ConcurrentDictionary, `RegisterDelegateFactory<T>()`, and `OfDelegate<T>()` to Mock.cs in TUnit.Mocks/Mock.cs

### Code Generation

- [ ] T022 [US2] Create MockDelegateBuilder that generates: delegate lambda dispatching through MockEngine, setup/verify extension methods for Invoke signature, factory registration with [ModuleInitializer] in TUnit.Mocks.SourceGenerator/Builders/MockDelegateBuilder.cs

### Tests

- [ ] T023 [US2] Create delegate mock tests in TUnit.Mocks.Tests/DelegateMockTests.cs — test Func<T,TResult>, Action<T>, custom delegate types, returns, throws, callbacks, argument capture, verification, strict mode
- [ ] T024 [US2] Update comparison matrix — change "Delegates" from No to Yes in docs/docs/comparison/mocking-frameworks.md
- [ ] T025 [US2] Update snapshot .verified.txt files in TUnit.Mocks.SourceGenerator.Tests/Snapshots/

**Checkpoint**: Delegate mocking works with full setup/verify

---

## Phase 5: US3 — Wrap Real Object (Priority: P3)

**Goal**: `Mock.Wrap<T>(realInstance)` wraps an existing object, delegating un-configured calls to the real implementation

**Independent Test**: Wrap a real service, override one method, verify non-overridden methods call real implementation

### Source Generator Changes

- [ ] T026 [US3] Add `IsMockWrapInvocation()` syntax predicate to MockTypeDiscovery in TUnit.Mocks.SourceGenerator/Discovery/MockTypeDiscovery.cs — detect `Mock.Wrap<T>()` calls
- [ ] T027 [US3] Extend MockImplBuilder to generate `_wrappedInstance` field and dispatch to wrapped instance for un-configured calls in TUnit.Mocks.SourceGenerator/Builders/MockImplBuilder.cs
- [ ] T028 [US3] Extend MockFactoryBuilder to generate wrap factory registration and constructor accepting wrapped instance in TUnit.Mocks.SourceGenerator/Builders/MockFactoryBuilder.cs

### Runtime Changes

- [ ] T029 [US3] Add `_wrapFactories` ConcurrentDictionary, `RegisterWrapFactory<T>()`, and `Wrap<T>(T instance)` to Mock.cs in TUnit.Mocks/Mock.cs

### Tests

- [ ] T030 [US3] Create wrap tests in TUnit.Mocks.Tests/WrapRealObjectTests.cs — test delegate to real, override specific method, verify all calls recorded, error on sealed type, error on null instance
- [ ] T031 [US3] Update comparison matrix — change "Wrap real object" from No to Yes in docs/docs/comparison/mocking-frameworks.md
- [ ] T032 [US3] Update snapshot .verified.txt files in TUnit.Mocks.SourceGenerator.Tests/Snapshots/

**Checkpoint**: Wrap real object works end-to-end

---

## Phase 6: US1 — Recursive/Auto Mocking (Priority: P1)

**Goal**: When a mocked method returns an interface and no setup is configured, automatically return a functional mock (not null)

**Independent Test**: Create a mock, call method returning interface without setup, verify non-null functional mock returned

### Source Generator Changes

- [ ] T033 [US1] Add `IsInterfaceReturnType` flag to MockMemberModel in TUnit.Mocks.SourceGenerator/Models/MockMemberModel.cs
- [ ] T034 [US1] Set `IsInterfaceReturnType` during member discovery in TUnit.Mocks.SourceGenerator/Discovery/MemberDiscovery.cs — check if return type (unwrapped for async) is an interface
- [ ] T035 [US1] Add transitive interface return type discovery to MockTypeDiscovery — walk method/property return types of mocked interfaces, yield additional MockTypeModel instances, limit recursion depth to 3, deduplicate by FQN in TUnit.Mocks.SourceGenerator/Discovery/MockTypeDiscovery.cs
- [ ] T036 [US1] Expand generator pipeline in MockGenerator to include transitive types — combine direct and transitive models, deduplicate before generation in TUnit.Mocks.SourceGenerator/MockGenerator.cs

### Runtime Changes

- [ ] T037 [US1] Add `ConcurrentDictionary<string, object> _autoMockCache` to MockEngine in TUnit.Mocks/MockEngine.cs
- [ ] T038 [US1] In `HandleCallWithReturn`, when no setup matches and return type has a registered factory, create and cache auto-mock via `Mock.TryCreateAutoMock()` — only in Loose mode in TUnit.Mocks/MockEngine.cs
- [ ] T039 [US1] Add `TryCreateAutoMock(Type type, MockBehavior behavior, out object mock)` to Mock.cs that checks factory registry in TUnit.Mocks/Mock.cs
- [ ] T040 [US1] Add `GetAutoMock<TChild>()` to Mock<T> that retrieves the auto-mock wrapper from engine cache in TUnit.Mocks/MockOfT.cs

### Tests

- [ ] T041 [US1] Create auto-mock tests in TUnit.Mocks.Tests/AutoMockTests.cs — test interface return auto-mocked, same instance on repeated calls, auto-mock configurable via GetAutoMock, nested chains (A→B→C), circular reference handling, strict mode throws (no auto-mock), async method returning Task<IInterface>
- [ ] T042 [US1] Update comparison matrix — change "Recursive/auto mocking" from No to Yes in docs/docs/comparison/mocking-frameworks.md
- [ ] T043 [US1] Update snapshot .verified.txt files in TUnit.Mocks.SourceGenerator.Tests/Snapshots/

**Checkpoint**: Recursive/auto mocking works with full chain support

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final validation and documentation

- [ ] T044 Verify all "No" entries for TUnit.Mocks in comparison matrix are now "Yes" (except sealed classes, static methods, LINQ to Mocks) in docs/docs/comparison/mocking-frameworks.md
- [ ] T045 Update Summary section in comparison doc to reflect full feature parity in docs/docs/comparison/mocking-frameworks.md
- [ ] T046 Run full test suite across all 3 test projects — verify zero regressions
- [ ] T047 [P] Run AOT compatibility check — `dotnet build TUnit.Mocks/TUnit.Mocks.csproj -c Release` with no warnings
- [ ] T048 [P] Run analyzer tests — verify all pass in TUnit.Mocks.Analyzers.Tests/
- [ ] T049 Update mocking guide page in docs/docs/test-authoring/mocking.md with brief overview of new features

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Foundational)**: No dependencies — start immediately
- **Phase 2 (US5)**: Depends on Phase 1 (needs IRaisable for future phases, but US5 itself only needs MockEngine changes)
- **Phase 3 (US4)**: Depends on Phase 1 (needs IRaisable interface and generated RaiseEvent method)
- **Phase 4 (US2)**: Independent — no dependencies on other phases
- **Phase 5 (US3)**: Independent — no dependencies on other phases
- **Phase 6 (US1)**: Independent — no dependencies on other phases
- **Phase 7 (Polish)**: Depends on all previous phases

### User Story Independence

- **US5 (Event subscription setup)**: Fully independent
- **US4 (Auto-raise event)**: Depends on IRaisable from Phase 1
- **US2 (Delegate mocking)**: Fully independent
- **US3 (Wrap real object)**: Fully independent
- **US1 (Recursive/auto mocking)**: Fully independent

### Parallel Opportunities

After Phase 1 completes:
- US5 + US2 + US3 + US1 can all proceed in parallel (different files, independent features)
- US4 can start after Phase 1 (needs IRaisable)

Within each user story:
- Source generator changes and runtime changes can be developed in parallel
- Tests depend on both generator and runtime changes

---

## Implementation Strategy

### MVP First (US5 + US4)

1. Complete Phase 1: Foundational
2. Complete Phase 2: US5 (Event subscription setup) — small, low risk
3. Complete Phase 3: US4 (Auto-raise event) — small, builds on Phase 1
4. **STOP and VALIDATE**: All 380+ tests pass, 2 new feature areas covered

### Incremental Delivery

1. Phase 1 → Foundation ready
2. Phase 2 → US5 done → validate
3. Phase 3 → US4 done → validate
4. Phase 4 → US2 done → validate (delegate mocking)
5. Phase 5 → US3 done → validate (wrap real object)
6. Phase 6 → US1 done → validate (auto mocking — highest complexity last)
7. Phase 7 → Polish → full parity achieved

---

## Notes

- Implementation order is bottom-up by complexity (easiest first), not by user story priority
- This lets us build confidence with the simpler features before tackling recursive auto-mocking
- Each phase is a complete increment — can stop and ship at any checkpoint
- All source generator changes require snapshot test updates
- Total: 49 tasks across 7 phases
