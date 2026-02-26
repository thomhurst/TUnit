# Tips & Pitfalls

TUnit-specific tips to avoid common mistakes.

## Always Await Assertions

TUnit assertions are async and return `Task`. Forgetting `await` means the assertion never executes — the test passes silently:

```csharp
// Wrong: assertion is never checked
[Test]
public async Task MyTest()
{
    Assert.That(result).IsEqualTo(5);  // passes without checking!
}

// Correct: assertion is awaited
[Test]
public async Task MyTest()
{
    await Assert.That(result).IsEqualTo(5);
}
```

The compiler warns about unawaited tasks, but this remains the most common TUnit mistake.

See [Awaiting Assertions](../assertions/awaiting.md) for details.

## New Instance Per Test

TUnit creates a **new instance** of your test class for every test method. Instance fields are never shared between tests:

```csharp
public class MyTests
{
    private int _value;  // reset to 0 for every test

    [Test, NotInParallel]
    public void Test1()
    {
        _value = 99;
    }

    [Test, NotInParallel]
    public async Task Test2()
    {
        // Fails! _value is 0 — this is a different instance
        await Assert.That(_value).IsEqualTo(99);
    }
}
```

If you genuinely need shared state, use `static` fields — but prefer making tests independent or using `[ClassDataSource<>]` instead.

## Use [DependsOn] for Test Ordering

When tests must run in a specific order, use `[DependsOn]`. Unlike `[NotInParallel(Order = N)]`, it preserves parallelism for unrelated tests:

```csharp
[Test]
public async Task Step1_CreateUser()
{
    // Runs first
}

[Test]
[DependsOn(nameof(Step1_CreateUser))]
public async Task Step2_UpdateUser()
{
    // Runs after Step1 completes
    // Other unrelated tests still run in parallel
}

[Test]
[DependsOn(nameof(Step2_UpdateUser))]
public async Task Step3_DeleteUser()
{
    // Runs after Step2 completes
}
```

`[DependsOn]` explicitly declares dependencies and supports depending on multiple tests. If you find yourself ordering many tests, consider whether they should be a single test or use proper setup/teardown instead.

See [Parallelism](../execution/parallelism.md) for `[NotInParallel]`, parallel groups, and concurrency configuration.

## Sharing Expensive Resources

For expensive setup shared across tests (web servers, databases, containers), use `[ClassDataSource<>]` with `IAsyncInitializer` and `IAsyncDisposable`:

```csharp
public class TestWebServer : IAsyncInitializer, IAsyncDisposable
{
    public WebApplicationFactory<Program>? Factory { get; private set; }

    public async Task InitializeAsync()
    {
        Factory = new WebApplicationFactory<Program>();
        await Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (Factory != null)
            await Factory.DisposeAsync();
    }
}

[ClassDataSource<TestWebServer>(Shared = SharedType.PerTestSession)]
public class ApiTests(TestWebServer server)
{
    [Test]
    public async Task Can_call_endpoint()
    {
        var client = server.Factory!.CreateClient();
        var response = await client.GetAsync("/api/health");
        await Assert.That(response.IsSuccessStatusCode).IsTrue();
    }

    [Test]
    public async Task Can_get_users()
    {
        var client = server.Factory!.CreateClient();
        var response = await client.GetAsync("/api/users");
        await Assert.That(response.IsSuccessStatusCode).IsTrue();
    }
}
```

This approach gives you type-safe constructor injection, automatic lifecycle management, and cross-class sharing via `SharedType`.

See [ClassDataSource](../writing-tests/class-data-source.md) for all sharing options.

## Choosing the Right Hook Level

- **`[Before(Test)]` / `[After(Test)]`** — runs before/after each test (most common)
- **`[Before(Class)]` / `[After(Class)]`** — runs once per test class
- **`[Before(Assembly)]` / `[After(Assembly)]`** — runs once per test assembly

For shared resources, prefer `[ClassDataSource<>]` over class/assembly hooks — it handles lifecycle automatically and works across multiple test classes.
