# Test Set Ups

Most setup for a test can be performed in the constructor (think setting up mocks, assigning fields.)

However some scenarios require further setup that could be an asynchronous operation.
E.g. pinging a service to wake it up in preparation for the tests.

For this, we can declare a method with a `[Before(...)]` or a `[BeforeEvery(...)]` attribute.

## Hook Method Signatures

Hook methods can be either synchronous or asynchronous:

```csharp
[Before(Test)]
public void SynchronousSetup()  // ✅ Valid - synchronous hook
{
    _value = 99;
}

[Before(Test)]
public async Task AsyncSetup()  // ✅ Valid - asynchronous hook
{
    _response = await new HttpClient().GetAsync("https://localhost/ping");
}
```

**Important Notes:**
- Hooks can be `void` (synchronous) or `async Task` (asynchronous)
- Use async hooks when you need to perform async operations (HTTP calls, database queries, etc.)
- Use synchronous hooks for simple setup (setting fields, initializing values, etc.)
- `async void` hooks are **not allowed** and will cause a compiler error

### Hook Parameters

Hooks can optionally accept parameters for accessing context information and cancellation tokens:

```csharp
[Before(Test)]
public async Task Setup(TestContext context, CancellationToken cancellationToken)
{
    // Access test information via context
    Console.WriteLine($"Setting up test: {context.TestDetails.TestName}");

    // Use cancellation token for timeout-aware operations
    await SomeLongRunningOperation(cancellationToken);
}

[Before(Class)]
public static async Task ClassSetup(ClassHookContext context, CancellationToken cancellationToken)
{
    // Both context and cancellation token available for class-level hooks
    await InitializeResources(cancellationToken);
}

[Before(Test)]
public async Task SetupWithToken(CancellationToken cancellationToken)
{
    // Can use CancellationToken without context
    await Task.Delay(100, cancellationToken);
}

[Before(Test)]
public async Task SetupWithContext(TestContext context)
{
    // Can use context without CancellationToken
    Console.WriteLine(context.TestDetails.TestName);
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
| `[Before(Test)]` | `TestContext` | Access test details, output writer |
| `[Before(Class)]` | `ClassHookContext` | Access class information |
| `[Before(Assembly)]` | `AssemblyHookContext` | Access assembly information |
| `[Before(TestSession)]` | `TestSessionContext` | Access test session information |
| `[Before(TestDiscovery)]` | `BeforeTestDiscoveryContext` | Access discovery context |

## [Before(HookType)]

### [Before(Test)]
Must be an instance method. Will be executed before each test in the class it's defined in.
Methods will be executed bottom-up, so the base class set ups will execute first and then the inheriting classes.

### [Before(Class)]
Must be a static method. Will run once before the first test in the class it's defined in starts.

### [Before(Assembly)]
Must be a static method. Will run once before the first test in the assembly it's defined in starts.

### [Before(TestSession)]
Must be a static method. Will run once before the first test in the test session starts.

### [Before(TestDiscovery)]
Must be a static method. Will run once before any tests are discovered.

## [BeforeEvery(HookType)]
All [BeforeEvery(...)] methods must be static - And should ideally be placed in their own file that's easy to find, as they can globally affect the test suite, so it should be easy for developers to locate this behaviour.
e.g. `GlobalHooks.cs` at the root of the test project.

### [BeforeEvery(Test)]
Will be executed before every test that will run in the test session.

### [BeforeEvery(Class)]
Will be executed before the first test of every class that will run in the test session.

### [BeforeEvery(Assembly)]
Will be executed before the first test of every assembly that will run in the test session.

### [BeforeEvery(TestSession)]
The same as [Before(TestSession)]

### [BeforeEvery(TestDiscovery)]
The same as [Before(TestDiscovery)]

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
## Common Mistakes & Best Practices

import Tabs from '@theme/Tabs';
import TabItem from '@theme/TabItem';

### Confusing Instance vs Static Hooks

<Tabs>
  <TabItem value="bad" label="❌ Bad - Wrong Hook Scope" default>

```csharp
public class DatabaseTests
{
    // ❌ Won't compile - Class-level hooks must be static
    [Before(Class)]
    public async Task SetupDatabase()
    {
        await InitializeDatabaseAsync();
    }

    // ❌ Won't compile - Test hooks cannot be static
    [Before(Test)]
    public static void SetupTest()
    {
        // Cannot access instance fields
    }
}
```

**Problem:** Hook scope (instance/static) must match the hook level.

  </TabItem>
  <TabItem value="good" label="✅ Good - Correct Hook Scopes">

```csharp
public class DatabaseTests
{
    // ✅ Class hooks must be static
    [Before(Class)]
    public static async Task SetupDatabase()
    {
        await InitializeDatabaseAsync();
    }

    // ✅ Test hooks must be instance methods
    [Before(Test)]
    public void SetupTest()
    {
        _testData = CreateTestData();
    }
}
```

**Why:** Class-level hooks run once and cannot access instance state. Test-level hooks run per test and can access instance fields.

  </TabItem>
</Tabs>

### Mixing Sync and Async Incorrectly

<Tabs>
  <TabItem value="bad" label="❌ Bad - Async Void">

```csharp
// ❌ Won't compile - async void is not allowed
[Before(Test)]
public async void SetupAsync()
{
    await Task.Delay(100);
}

// ❌ Blocking on async code
[Before(Test)]
public void Setup()
{
    SomeAsyncMethod().Wait(); // Can cause deadlocks
}
```

**Problem:** Async void can't be awaited and blocking async code can cause deadlocks.

  </TabItem>
  <TabItem value="good" label="✅ Good - Proper Async">

```csharp
// ✅ Use async Task for asynchronous operations
[Before(Test)]
public async Task SetupAsync()
{
    await Task.Delay(100);
}

// ✅ Use synchronous method for synchronous work
[Before(Test)]
public void Setup()
{
    _value = 42;
}
```

**Why:** `async Task` allows proper awaiting and error handling. Synchronous hooks are fine for non-async work.

  </TabItem>
</Tabs>

### Expensive Setup at Wrong Level

<Tabs>
  <TabItem value="bad" label="❌ Bad - Repeated Expensive Setup">

```csharp
public class ApiTests
{
    private HttpClient _client;

    // ❌ Creates new client for EVERY test
    [Before(Test)]
    public void Setup()
    {
        _client = new HttpClient
        {
            BaseAddress = new Uri("https://api.example.com")
        };
    }

    [Test]
    public async Task Test1() { /* ... */ }

    [Test]
    public async Task Test2() { /* ... */ }
    // Client created 2 times unnecessarily
}
```

**Problem:** Creating expensive resources per test wastes time and resources.

  </TabItem>
  <TabItem value="good" label="✅ Good - Shared Setup">

```csharp
public class ApiTests
{
    private static HttpClient _client;

    // ✅ Creates client once for all tests
    [Before(Class)]
    public static void SetupOnce()
    {
        _client = new HttpClient
        {
            BaseAddress = new Uri("https://api.example.com")
        };
    }

    [After(Class)]
    public static void CleanupOnce()
    {
        _client?.Dispose();
    }

    [Test]
    public async Task Test1() { /* ... */ }

    [Test]
    public async Task Test2() { /* ... */ }
    // Client created only once
}
```

**Why:** Class-level setup runs once, sharing expensive resources across tests. Much faster!

  </TabItem>
</Tabs>

## AsyncLocal

If you are wanting to set AsyncLocal values within your `[Before(...)]` hooks, this is supported.

But to propagate the values into the test framework, you must call `context.AddAsyncLocalValues()` - Where `context` is the relevant context object injected into your hook method.

E.g.

```csharp
    [BeforeEvery(Class)]
    public static void BeforeClass(ClassHookContext context)
    {
        _myAsyncLocal.Value = "Some Value";
        context.AddAsyncLocalValues();
    }
```
