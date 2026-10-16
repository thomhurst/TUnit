
# Hooks

Hooks let you run code at specific points in the test lifecycle using `[Before]` / `[BeforeEvery]` and `[After]` / `[AfterEvery]` attributes. Most simple setup belongs in the constructor; use hooks for async operations or shared resource management.

For the full execution order, see [Test Lifecycle](lifecycle.md).

## Hook Method Signatures

Hook methods can be synchronous or asynchronous:

```csharp
[Before(Test)]
public void SynchronousSetup()  // ✅ Valid
{
    _value = 99;
}

[Before(Test)]
public async Task AsyncSetup()  // ✅ Valid
{
    _response = await new HttpClient().GetAsync("https://localhost/ping");
}

[After(Test)]
public void SynchronousCleanup()  // ✅ Valid
{
    _resource?.Dispose();
}

[After(Test)]
public async Task AsyncCleanup()  // ✅ Valid
{
    await NotifyTestFinished();
}
```

- Hooks can be `void` (synchronous) or `async Task` (asynchronous)
- `async void` hooks are **not allowed** and will cause a compiler error

## Hook Parameters

Hooks can optionally accept a context object and/or a `CancellationToken`:

```csharp
[Before(Test)]
public async Task Setup(TestContext context, CancellationToken cancellationToken)
{
    Console.WriteLine($"Setting up: {context.Metadata.TestName}");
    await SomeLongRunningOperation(cancellationToken);
}
```

**Valid parameter combinations:**
- No parameters: `public void Hook() { }`
- Context only: `public void Hook(TestContext context) { }`
- CancellationToken only: `public async Task Hook(CancellationToken ct) { }`
- Both: `public async Task Hook(TestContext context, CancellationToken ct) { }`

In setup hooks, pass the injected `CancellationToken` to setup operations, including operations returned directly from a hook. It observes the hook's `[Timeout]` or `TUnitSettings.Default.Timeouts.DefaultHookTimeout` when no explicit timeout is set. `context.Execution.CancellationToken` represents test cancellation and does not include that hook timeout, so using it for setup can leave the operation running after the hook's token is cancelled.

[TUnit0075](../reference/tunit0075.md) warns about directly passing the test context's token to an operation awaited or returned by a `[Before(Test)]` or `[BeforeEvery(Test)]` hook, including conditional branches and overrides of virtual setup hooks. Reading test cancellation state, such as `context.Execution.CancellationToken.IsCancellationRequested`, is still valid.

**Context types by hook level:**

| Hook Level | Context Type |
|------------|-------------|
| `[Before(Test)]` / `[After(Test)]` | `TestContext` |
| `[Before(Class)]` / `[After(Class)]` | `ClassHookContext` |
| `[Before(Assembly)]` / `[After(Assembly)]` | `AssemblyHookContext` |
| `[Before(TestSession)]` / `[After(TestSession)]` | `TestSessionContext` |
| `[Before(TestDiscovery)]` | `BeforeTestDiscoveryContext` |
| `[After(TestDiscovery)]` | `TestDiscoveryContext` |
| `[Before(TestFiltering)]` / `[After(TestFiltering)]` | `TestFilteringContext` |

### Checking Test Results in Cleanup

A common pattern in `[After]` hooks is checking whether the test failed:

```csharp
[After(Test)]
public async Task Cleanup(TestContext context, CancellationToken cancellationToken)
{
    if (context.Execution.Result?.State == TestState.Failed)
    {
        await CaptureScreenshot(cancellationToken);
    }
}
```

## Setup Hooks: [Before] and [BeforeEvery]

### [Before(HookType)]

| Level | Method Type | Scope |
|-------|------------|-------|
| `[Before(Test)]` | Instance | Before each test in the declaring class. Base class hooks run first (bottom-up). |
| `[Before(Class)]` | Static | Once before the first test in the declaring class. |
| `[Before(Assembly)]` | Static | Once before the first test in the assembly. |
| `[Before(TestSession)]` | Static | Once before the first test in the session. |
| `[Before(TestDiscovery)]` | Static | Once before any tests are discovered. |
| `[Before(TestFiltering)]` | Static | Once after framework filtering has selected the tests to run, before the session starts. Can exclude, skip and order tests. |

### [BeforeEvery(HookType)]

All `[BeforeEvery]` methods must be **static**. Place them in a dedicated file (e.g., `GlobalHooks.cs`) since they globally affect the test suite.

| Level | Scope |
|-------|-------|
| `[BeforeEvery(Test)]` | Before **every** test in the session |
| `[BeforeEvery(Class)]` | Before the first test of **every** class |
| `[BeforeEvery(Assembly)]` | Before the first test of **every** assembly |

### Setup Example

```csharp
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass
{
    private int _value;
    private static HttpResponseMessage? _pingResponse;

    [Before(Class)]
    public static async Task Ping()
    {
        _pingResponse = await new HttpClient().GetAsync("https://localhost/ping");
    }

    [Before(Test)]
    public async Task Setup()
    {
        await Task.CompletedTask;
        _value = 99;
    }

    [Test]
    public async Task MyTest()
    {
        await Assert.That(_value).IsEqualTo(99);
        await Assert.That(_pingResponse?.StatusCode)
            .IsNotNull()
            .And.IsEqualTo(HttpStatusCode.OK);
    }
}
```

## Cleanup Hooks: [After] and [AfterEvery]

TUnit also supports `IDisposable` and `IAsyncDisposable` on test classes, but `[After]` attributes are preferred — they support multiple methods and collect exceptions from all of them, throwing lazily afterwards.

### [After(HookType)]

| Level | Method Type | Scope |
|-------|------------|-------|
| `[After(Test)]` | Instance | After each test. Current class hooks run first (top-down). |
| `[After(Class)]` | Static | Once after the last test in the declaring class. |
| `[After(Assembly)]` | Static | Once after the last test in the assembly. |
| `[After(TestSession)]` | Static | Once after the last test in the session. |
| `[After(TestDiscovery)]` | Static | Once after tests are discovered. Can exclude, skip and order tests. |
| `[After(TestFiltering)]` | Static | Once with the final set of tests to run. Read-only. |

### [AfterEvery(HookType)]

All `[AfterEvery]` methods must be **static**. Place them in their own file (e.g., `GlobalHooks.cs`).

:::info
Use `[AfterEvery(...)]` for global cleanup logic that should run after every test/class/assembly/session, regardless of where the test is defined.
:::

| Level | Scope |
|-------|-------|
| `[AfterEvery(Test)]` | After **every** test in the session |
| `[AfterEvery(Class)]` | After the last test of **every** class |
| `[AfterEvery(Assembly)]` | After the last test of **every** assembly |

### Cleanup Example

```csharp
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass
{
    [After(Class)]
    public static async Task KillChromedrivers()
    {
        await Task.CompletedTask;

        foreach (var process in Process.GetProcessesByName("chromedriver.exe"))
        {
            process.Kill();
        }
    }

    [After(Test)]
    public async Task AfterEachTest()
    {
        await new HttpClient().GetAsync($"https://localhost/test-finished-notifier?testName={TestContext.Current!.Metadata.TestName}");
    }

    [Test]
    public async Task MyTest()
    {
        // Do something
    }
}
```

## Excluding, Skipping and Ordering Tests

`[After(TestDiscovery)]` and `[Before(TestFiltering)]` hooks can change which tests run and in what order:

- `ExcludeTests(predicate)` removes tests from the run. Excluded tests are not reported.
- `SkipTests(predicate, reason)` keeps tests in the run but reports them as skipped with the reason.
- `OrderTestsBy(keySelector, comparer)` orders the tests by a key.

```csharp
using TUnit.Core;

namespace MyTestProject;

public static class RunSelectionHooks
{
    private static readonly bool IsNightlyRun = Environment.GetEnvironmentVariable("NIGHTLY") == "true";

    [After(TestDiscovery)]
    public static void OnDiscovery(TestDiscoveryContext context)
    {
        // Hidden from the run and from test discovery (IDE, --list-tests).
        context.ExcludeTests(t => t.Metadata.TestDetails.Categories.Contains("Nightly") && !IsNightlyRun);
    }

    [Before(TestFiltering)]
    public static void BeforeFiltering(TestFilteringContext context)
    {
        // context.AllTests holds the tests that passed --treenode-filter and [Explicit] handling.
        context.SkipTests(t => t.Metadata.TestDetails.Categories.Contains("Docker") && !IsDockerAvailable(),
            "Docker is not available");

        context.OrderTestsBy(t => t.Metadata.TestName, StringComparer.Ordinal);
    }

    [After(TestFiltering)]
    public static void AfterFiltering(TestFilteringContext context)
    {
        Console.WriteLine($"Running {context.AllTests.Count} tests");
    }

    private static bool IsDockerAvailable() => File.Exists("/var/run/docker.sock");
}
```

The stages run in this order: `[Before(TestDiscovery)]`, discovery, `[After(TestDiscovery)]`, framework filtering, `[Before(TestFiltering)]`, `[After(TestFiltering)]`, then the test session.

- **Discovery vs. filtering:** exclusions in `[After(TestDiscovery)]` also apply to discovery requests, so the tests disappear from the IDE and `--list-tests`. `TestFiltering` hooks run only when tests are executed. Use them to decide what runs without hiding tests from discovery.
- **Dependencies:** if you exclude a test that a remaining test depends on (`[DependsOn]`), it still runs, as with `--treenode-filter`. Run with `--log-level Debug` to see which excluded tests were re-included this way. If you skip it, the tests that depend on it are skipped too, unless the dependency uses `ProceedOnFailure`.
- **Ordering:** the order decides when tests *start*. Tests with equal keys keep the default order among themselves (by class, then `[NotInParallel(Order)]`). So `OrderTestsBy(t => category)` runs the categories in order without breaking each class's `[NotInParallel(Order)]` sequence, and a key that differs for every test, such as `(category, t.Metadata.TestName)`, gives a total order. For tests that can run in parallel, `[ExecutionPriority]` still takes precedence, and those tests can still overlap. Use `[NotInParallel]` when the sequence matters. The order applies within each scheduling phase and never moves a test to another phase: the run still starts parallel and keyed `[NotInParallel("key")]` tests first, then each `[ParallelGroup]`, then global `[NotInParallel]` tests. Calls don't compose like `ThenBy`: if several hooks call `OrderTestsBy`, the last call wins, so combine keys into one instead.
- **Dependency lookups:** tests excluded by a hook are not returned by `TestContext.Current.Dependencies.GetTests(...)`.
- **When changes take effect:** skips and ordering are applied when the hook stage ends, and only to tests still in the run, so skipping a test and then excluding it leaves it untouched. If a predicate or key selector throws, that call changes nothing.
- **When changes are allowed:** changing the tests after `[After(TestDiscovery)]` hooks have finished, or from an `[After(TestFiltering)]` hook, throws `InvalidOperationException`. This makes the *set* read-only; a hook can still change an individual test through its `TestContext`.
- Tests added at run time (dynamic tests) do not pass through these hooks.

## Common Mistakes

- **Instance vs static mismatch** — `[Before(Class)]` and higher must be `static`. `[Before(Test)]` must be an instance method. The compiler will error if you mix these up.
- **`async void`** — Not allowed. Use `async Task` for async hooks, or `void` for synchronous hooks.
- **Blocking on async** — Never call `.Wait()` or `.Result` inside a hook. Use `async Task` instead.
- **Expensive per-test setup** — If setup is expensive (HTTP clients, DB connections), use `[Before(Class)]` to run it once, or use `[ClassDataSource<T>]` for automatic lifecycle management.

## AsyncLocal

Setting `AsyncLocal` values in `[Before]` hooks is supported. Call `context.AddAsyncLocalValues()` to propagate them into the test framework:

```csharp
[BeforeEvery(Class)]
public static void BeforeClass(ClassHookContext context)
{
    _myAsyncLocal.Value = "Some Value";
    context.AddAsyncLocalValues();
}
```

## See Also

- [Test Lifecycle](lifecycle.md) — Full overview of the test execution lifecycle
- [Event Subscribing](event-subscribing.md) — Event receiver interfaces for advanced scenarios
