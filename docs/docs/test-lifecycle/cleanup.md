# Test Clean Ups

TUnit supports having your test class implement `IDisposable` or `IAsyncDisposable`. These will be called after your test has finished executing. However, using the attributes below offers better support for running multiple methods, and without having to implement your own try/catch logic. Every `[After]` method will be run, and any exceptions will be lazily thrown afterwards.

You can also declare a method with an `[After(...)]` or an `[AfterEvery(...)]` attribute.

## Hook Method Signatures

Hook methods can be either synchronous or asynchronous:

```csharp
[After(Test)]
public void SynchronousCleanup()  // ✅ Valid - synchronous hook
{
    _resource?.Dispose();
}

[After(Test)]
public async Task AsyncCleanup()  // ✅ Valid - asynchronous hook
{
    await new HttpClient().GetAsync("https://localhost/test-finished-notifier");
}
```

**Important Notes:**
- Hooks can be `void` (synchronous) or `async Task` (asynchronous)
- Use async hooks when you need to perform async operations (HTTP calls, database queries, etc.)
- Use synchronous hooks for simple cleanup (disposing objects, resetting state, etc.)
- `async void` hooks are **not allowed** and will cause a compiler error

### Hook Parameters

Hooks can optionally accept parameters for accessing context information and cancellation tokens:

```csharp
[After(Test)]
public async Task Cleanup(TestContext context, CancellationToken cancellationToken)
{
    // Access test results via context
    if (context.Result?.Status == TestStatus.Failed)
    {
        await CaptureScreenshot(cancellationToken);
    }
}

[After(Class)]
public static async Task ClassCleanup(ClassHookContext context, CancellationToken cancellationToken)
{
    // Use cancellation token for timeout-aware cleanup operations
    await DisposeResources(cancellationToken);
}

[After(Test)]
public async Task CleanupWithToken(CancellationToken cancellationToken)
{
    // Can use CancellationToken without context
    await FlushBuffers(cancellationToken);
}

[After(Test)]
public async Task CleanupWithContext(TestContext context)
{
    // Can use context without CancellationToken
    Console.WriteLine($"Test {context.TestDetails.TestName} completed");
}
```

**Valid Parameter Combinations:**
- No parameters: `public void Hook() { }`
- Context only: `public void Hook(TestContext context) { }`
- CancellationToken only: `public async Task Hook(CancellationToken ct) { }`
- Both: `public async Task Hook(TestContext context, CancellationToken ct) { }`

**Context Types by Hook Level:**

| Hook Level | Context Type | Example |
|------------|-------------|---------|
| `[After(Test)]` | `TestContext` | Access test results, output writer |
| `[After(Class)]` | `ClassHookContext` | Access class information |
| `[After(Assembly)]` | `AssemblyHookContext` | Access assembly information |
| `[After(TestSession)]` | `TestSessionContext` | Access test session information |
| `[After(TestDiscovery)]` | `TestDiscoveryContext` | Access discovery results |

## [After(HookType)]

### [After(Test)]
Must be an instance method. Will be executed after each test in the class it's defined in.
Methods will be executed top-down, so the current class clean ups will execute first, then the base classes' last.

### [After(Class)]
Must be a static method. Will run once after the last test in the class it's defined it finishes.

### [After(Assembly)]
Must be a static method. Will run once after the last test in the assembly it's defined it finishes.

### [After(TestSession)]
Must be a static method. Will run once after the last test in the test session finishes.

### [After(TestDiscovery)]
Must be a static method. Will run once after tests are discovered.

## [AfterEvery(HookType)]
All [AfterEvery(...)] methods must be static - And should ideally be placed in their own file that's easy to find, as they can globally affect the test suite, so it should be easy for developers to locate this behaviour.
e.g. `GlobalHooks.cs` at the root of the test project.

:::info
Use `[AfterEvery(...)]` for global clean-up logic that should run after every test/class/assembly/session, regardless of where the test is defined.
:::

### [AfterEvery(Test)]
Will be executed after every test that will run in the test session.

### [AfterEvery(Class)]
Will be executed after the last test of every class that will run in the test session.

### [AfterEvery(Assembly)]
Will be executed after the last test of every assembly that will run in the test session.

### [AfterEvery(TestSession)]
The same as [After(TestSession)]

### [AfterEvery(TestDiscovery)]
The same as [After(TestDiscovery)]

```csharp
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass
{
    private int _value;
    private static HttpResponseMessage? _pingResponse;

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
        await new HttpClient().GetAsync($"https://localhost/test-finished-notifier?testName={TestContext.Current.TestDetails.TestName}");
    }

    [Test]
    public async Task MyTest()
    {
        // Do something
    }
}
```
