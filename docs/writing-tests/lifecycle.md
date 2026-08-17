# Test Lifecycle Overview

TUnit provides multiple mechanisms for hooking into the test lifecycle:

1. **Hook Attributes** (`[Before]`, `[After]`, etc.) - Method-based hooks
2. **Event Receivers** (interfaces like `ITestStartEventReceiver`) - Object-based event subscriptions
3. **Initialization Interfaces** (`IAsyncInitializer`, `IAsyncDiscoveryInitializer`) - Async object setup
4. **Disposal Interfaces** (`IDisposable`, `IAsyncDisposable`) - Resource cleanup

## Complete Lifecycle Diagram[​](#complete-lifecycle-diagram "Direct link to Complete Lifecycle Diagram")

Discovery runs first, then every test executes inside nested scopes:

<!-- -->

`[Before]` hooks run outermost first (session → assembly → class → test), and `[After]` hooks unwind in the opposite order:

<!-- -->

Everything inside a single test — construction, initialization, hooks, body and disposal:

<!-- -->

## Phase 1: Test Discovery[​](#phase-1-test-discovery "Direct link to Phase 1: Test Discovery")

Before any tests execute, TUnit discovers all tests and prepares data sources.

<!-- -->

### Discovery Phase Details[​](#discovery-phase-details "Direct link to Discovery Phase Details")

| Step                         | What Happens                                                       |
| ---------------------------- | ------------------------------------------------------------------ |
| `[Before(TestDiscovery)]`    | Hook runs once before discovery begins                             |
| **Scan Assemblies**          | Find all methods with `[Test]` attribute                           |
| **Create Data Sources**      | Instantiate `ClassDataSource<T>`, resolve `MethodDataSource`, etc. |
| **Property Injection**       | Resolve and cache property values for data sources                 |
| `IAsyncDiscoveryInitializer` | Initialize objects that need to be ready during discovery          |
| `[After(TestDiscovery)]`     | Hook runs once after discovery completes                           |
| `OnTestRegistered`           | Event fires for each test after registration                       |

Discovery vs Execution

`IAsyncInitializer` does **NOT** run during discovery. Only `IAsyncDiscoveryInitializer` runs at discovery time.

Use `IAsyncDiscoveryInitializer` when your data source needs async initialization to generate test cases (e.g., loading test data from a database).

## Phase 2: Test Execution[​](#phase-2-test-execution "Direct link to Phase 2: Test Execution")

### Per-Test Execution Flow[​](#per-test-execution-flow "Direct link to Per-Test Execution Flow")

<!-- -->

### Complete Test Execution Order[​](#complete-test-execution-order "Direct link to Complete Test Execution Order")

Exact order of operations for a single test:

| Order | What Happens                                     | Type                                |
| ----- | ------------------------------------------------ | ----------------------------------- |
| 1     | `[Before(TestSession)]`                          | Hook (once per session)             |
| 2     | `IFirstTestInTestSessionEventReceiver`           | Event (once per session)            |
| 3     | `[BeforeEvery(Assembly)]` / `[Before(Assembly)]` | Hooks (once per assembly)           |
| 4     | `IFirstTestInAssemblyEventReceiver`              | Event (once per assembly)           |
| 5     | `[BeforeEvery(Class)]` / `[Before(Class)]`       | Hooks (once per class)              |
| 6     | `IFirstTestInClassEventReceiver`                 | Event (once per class)              |
| 7     | **Create test class instance**                   | Constructor runs                    |
| 8     | **Set property values on instance**              | Cached values applied               |
| 9     | **`IAsyncInitializer.InitializeAsync()`**        | All tracked objects initialized     |
| 10    | `[BeforeEvery(Test)]`                            | Hook                                |
| 11    | `ITestStartEventReceiver` (Early)                | Event                               |
| 12    | `[Before(Test)]`                                 | Hook (instance method)              |
| 13    | `ITestStartEventReceiver` (Late)                 | Event                               |
| 14    | **Test Body Execution**                          | Your test code runs                 |
| 15    | `ITestEndEventReceiver` (Early)                  | Event                               |
| 16    | `[After(Test)]`                                  | Hook (instance method)              |
| 17    | `ITestEndEventReceiver` (Late)                   | Event                               |
| 18    | `[AfterEvery(Test)]`                             | Hook                                |
| 19    | **`IAsyncDisposable` / `IDisposable`**           | Test instance disposed              |
| 20    | **Cleanup tracked objects**                      | Ref count decremented, dispose if 0 |
| 21    | `ILastTestInClassEventReceiver`                  | Event (after last test in class)    |
| 22    | `[After(Class)]` / `[AfterEvery(Class)]`         | Hooks (after last test in class)    |
| 23    | `ILastTestInAssemblyEventReceiver`               | Event (after last test in assembly) |
| 24    | `[After(Assembly)]` / `[AfterEvery(Assembly)]`   | Hooks (after last test in assembly) |
| 25    | `ILastTestInTestSessionEventReceiver`            | Event (after last test in session)  |
| 26    | `[After(TestSession)]`                           | Hook (once per session)             |

## Initialization Interfaces[​](#initialization-interfaces "Direct link to Initialization Interfaces")

### IAsyncInitializer vs IAsyncDiscoveryInitializer[​](#iasyncinitializer-vs-iasyncdiscoveryinitializer "Direct link to IAsyncInitializer vs IAsyncDiscoveryInitializer")

<!-- -->

| Interface                    | When It Runs                                    | Use Case                              |
| ---------------------------- | ----------------------------------------------- | ------------------------------------- |
| `IAsyncDiscoveryInitializer` | During test discovery                           | Loading data for test case generation |
| `IAsyncInitializer`          | During test execution (after `[Before(Class)]`) | Starting containers, DB connections   |

### Initialization Order[​](#initialization-order "Direct link to Initialization Order")

Objects are initialized **depth-first** (deepest nested objects first):

<!-- -->

```
// If TestClass has PropertyA, and PropertyA has PropertyB...

// Initialization order: PropertyB → PropertyA → TestClass
```

## Disposal Interfaces[​](#disposal-interfaces "Direct link to Disposal Interfaces")

### When Disposal Happens[​](#when-disposal-happens "Direct link to When Disposal Happens")

<!-- -->

### Disposal by Sharing Type[​](#disposal-by-sharing-type "Direct link to Disposal by Sharing Type")

| SharedType       | When Disposed                          |
| ---------------- | -------------------------------------- |
| `None` (default) | After each test                        |
| `PerClass`       | After last test in the class           |
| `PerAssembly`    | After last test in the assembly        |
| `PerTestSession` | After test session ends                |
| `Keyed`          | When all tests using that key complete |

## Property Injection Lifecycle[​](#property-injection-lifecycle "Direct link to Property Injection Lifecycle")

<!-- -->

### Key Points[​](#key-points "Direct link to Key Points")

1. **Property values are resolved once** during test registration
2. **Shared objects** (`PerClass`, `PerAssembly`, etc.) are created once and reused
3. **Each test gets a new instance** of the test class
4. **Cached values are set** on each new test instance
5. **`IAsyncInitializer`** runs after `[Before(Class)]` hooks

## Event Receiver Interfaces[​](#event-receiver-interfaces "Direct link to Event Receiver Interfaces")

### All Event Receiver Interfaces[​](#all-event-receiver-interfaces "Direct link to All Event Receiver Interfaces")

| Interface                              | When Fired                    | Context                 |
| -------------------------------------- | ----------------------------- | ----------------------- |
| `ITestRegisteredEventReceiver`         | After test discovered         | `TestRegisteredContext` |
| `IFirstTestInTestSessionEventReceiver` | Before first test in session  | `TestSessionContext`    |
| `IFirstTestInAssemblyEventReceiver`    | Before first test in assembly | `AssemblyHookContext`   |
| `IFirstTestInClassEventReceiver`       | Before first test in class    | `ClassHookContext`      |
| `ITestStartEventReceiver`              | When test begins              | `TestContext`           |
| `ITestEndEventReceiver`                | When test completes           | `TestContext`           |
| `ITestSkippedEventReceiver`            | When test is skipped          | `TestContext`           |
| `ILastTestInClassEventReceiver`        | After last test in class      | `ClassHookContext`      |
| `ILastTestInAssemblyEventReceiver`     | After last test in assembly   | `AssemblyHookContext`   |
| `ILastTestInTestSessionEventReceiver`  | After last test in session    | `TestSessionContext`    |

### Early vs Late Stage[​](#early-vs-late-stage "Direct link to Early vs Late Stage")

For `ITestStartEventReceiver` and `ITestEndEventReceiver`:

<!-- -->

```
public class MyAttribute : Attribute, ITestStartEventReceiver

{

    // Early = runs BEFORE [Before(Test)]

    // Late (default) = runs AFTER [Before(Test)]

    public EventReceiverStage Stage => EventReceiverStage.Early;



    public ValueTask OnTestStart(TestContext context) => ValueTask.CompletedTask;

}
```

## Hook Attributes Reference[​](#hook-attributes-reference "Direct link to Hook Attributes Reference")

### All Hook Types[​](#all-hook-types "Direct link to All Hook Types")

| Level          | Before                    | After                    | Method Type  |
| -------------- | ------------------------- | ------------------------ | ------------ |
| Test Discovery | `[Before(TestDiscovery)]` | `[After(TestDiscovery)]` | Static       |
| Test Session   | `[Before(TestSession)]`   | `[After(TestSession)]`   | Static       |
| Assembly       | `[Before(Assembly)]`      | `[After(Assembly)]`      | Static       |
| Class          | `[Before(Class)]`         | `[After(Class)]`         | Static       |
| Test           | `[Before(Test)]`          | `[After(Test)]`          | **Instance** |

### Before vs BeforeEvery[​](#before-vs-beforeevery "Direct link to Before vs BeforeEvery")

| Attribute              | Scope                              |
| ---------------------- | ---------------------------------- |
| `[Before(Class)]`      | Once for **this class only**       |
| `[BeforeEvery(Class)]` | Before **every class** in session  |
| `[Before(Test)]`       | Before **each test in this class** |
| `[BeforeEvery(Test)]`  | Before **every test** in session   |

## Quick Reference[​](#quick-reference "Direct link to Quick Reference")

```
┌─ DISCOVERY ──────────────────────────────────────────────────────┐

│ [Before(TestDiscovery)]                                          │

│ → Scan assemblies for [Test] methods                             │

│ → Create data sources, inject properties                         │

│ → IAsyncDiscoveryInitializer.InitializeAsync()                   │

│ [After(TestDiscovery)]                                           │

│ → ITestRegisteredEventReceiver.OnTestRegistered (per test)       │

└──────────────────────────────────────────────────────────────────┘

                              │

                              ▼

┌─ TEST SESSION ───────────────────────────────────────────────────┐

│ [Before(TestSession)] → IFirstTestInTestSessionEventReceiver     │

│   │                                                               │

│   ├─ [Before(Assembly)] → IFirstTestInAssemblyEventReceiver      │

│   │   │                                                           │

│   │   ├─ [Before(Class)] → IFirstTestInClassEventReceiver        │

│   │   │   │                                                       │

│   │   │   │  ┌─ PER TEST ─────────────────────────────────────┐  │

│   │   │   │  │ Create instance (constructor)                   │  │

│   │   │   │  │ Set property values                             │  │

│   │   │   │  │ IAsyncInitializer.InitializeAsync()             │  │

│   │   │   │  │ [BeforeEvery(Test)]                             │  │

│   │   │   │  │ ITestStartEventReceiver (Early)                 │  │

│   │   │   │  │ [Before(Test)]                                  │  │

│   │   │   │  │ ITestStartEventReceiver (Late)                  │  │

│   │   │   │  │ ─────────── TEST BODY ───────────               │  │

│   │   │   │  │ ITestEndEventReceiver (Early)                   │  │

│   │   │   │  │ [After(Test)]                                   │  │

│   │   │   │  │ ITestEndEventReceiver (Late)                    │  │

│   │   │   │  │ [AfterEvery(Test)]                              │  │

│   │   │   │  │ IAsyncDisposable / IDisposable                  │  │

│   │   │   │  │ Cleanup tracked objects                         │  │

│   │   │   │  └─────────────────────────────────────────────────┘  │

│   │   │   │                                                       │

│   │   │   ├─ ILastTestInClassEventReceiver → [After(Class)]      │

│   │   │                                                           │

│   │   ├─ ILastTestInAssemblyEventReceiver → [After(Assembly)]    │

│   │                                                               │

│   ├─ ILastTestInTestSessionEventReceiver → [After(TestSession)]  │

└───────────────────────────────────────────────────────────────────┘
```

## Exception Handling[​](#exception-handling "Direct link to Exception Handling")

Cleanup Always Runs

All `[After]` hooks, `ITestEndEventReceiver` events, and disposal methods run even if earlier ones fail. Exceptions are collected and thrown together.

| Phase        | Behavior                              |
| ------------ | ------------------------------------- |
| Before hooks | Fail fast (exception stops execution) |
| After hooks  | Run all, collect exceptions           |
| Disposal     | Always runs, exceptions collected     |

## Related Pages[​](#related-pages "Direct link to Related Pages")

* [Hooks](/docs/writing-tests/hooks.md) - Detailed guide to `[Before]` and `[After]` hooks
* [Event Subscribing](/docs/writing-tests/event-subscribing.md) - Event receiver interfaces
* [Property Injection](/docs/writing-tests/property-injection.md) - Property injection and `IAsyncInitializer`
* [Dependency Injection](/docs/writing-tests/dependency-injection.md) - DI integration
* [Test Context](/docs/writing-tests/test-context.md) - Accessing test information
