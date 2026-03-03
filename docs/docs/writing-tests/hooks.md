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

**Context types by hook level:**

| Hook Level | Context Type |
|------------|-------------|
| `[Before(Test)]` / `[After(Test)]` | `TestContext` |
| `[Before(Class)]` / `[After(Class)]` | `ClassHookContext` |
| `[Before(Assembly)]` / `[After(Assembly)]` | `AssemblyHookContext` |
| `[Before(TestSession)]` / `[After(TestSession)]` | `TestSessionContext` |
| `[Before(TestDiscovery)]` / `[After(TestDiscovery)]` | `BeforeTestDiscoveryContext` |

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
| `[After(TestDiscovery)]` | Static | Once after tests are discovered. |

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
        await new HttpClient().GetAsync($"https://localhost/test-finished-notifier?testName={TestContext.Current.Metadata.TestName}");
    }

    [Test]
    public async Task MyTest()
    {
        // Do something
    }
}
```

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
