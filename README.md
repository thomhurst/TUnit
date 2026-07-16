![TUnit](assets/banner.png)

# TUnit

A modern .NET testing framework. Tests are discovered at compile time via source generators, run in parallel by default, and work under Native AOT — all built on [Microsoft.Testing.Platform](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-intro).

<div align="center">

[![thomhurst%2FTUnit | Trendshift](https://trendshift.io/api/badge/repositories/11781)](https://trendshift.io/repositories/11781)

[![Codacy Badge](https://api.codacy.com/project/badge/Grade/a8231644d844435eb9fd15110ea771d8)](https://app.codacy.com/gh/thomhurst/TUnit?utm_source=github.com&utm_medium=referral&utm_content=thomhurst/TUnit&utm_campaign=Badge_Grade) ![GitHub Repo stars](https://img.shields.io/github/stars/thomhurst/TUnit) ![GitHub Issues or Pull Requests](https://img.shields.io/github/issues-closed-raw/thomhurst/TUnit)
 [![GitHub Sponsors](https://img.shields.io/github/sponsors/thomhurst)](https://github.com/sponsors/thomhurst) [![nuget](https://img.shields.io/nuget/v/TUnit.svg)](https://www.nuget.org/packages/TUnit/) [![NuGet Downloads](https://img.shields.io/nuget/dt/TUnit)](https://www.nuget.org/packages/TUnit/) ![GitHub Workflow Status (with event)](https://img.shields.io/github/actions/workflow/status/thomhurst/TUnit/dotnet.yml) ![GitHub last commit (branch)](https://img.shields.io/github/last-commit/thomhurst/TUnit/main) ![License](https://img.shields.io/github/license/thomhurst/TUnit)

</div>

## What it looks like

```csharp
[Test]
[Arguments("GOLD", 100.00, 80.00)]
[Arguments("SILVER", 100.00, 90.00)]
public async Task Discount_Is_Applied(string tier, double subtotal, double expected)
{
    var checkout = new CheckoutService();

    var total = await checkout.ApplyDiscountAsync(tier, subtotal);

    await Assert.That(total).IsEqualTo(expected);
}
```

When a test fails, TUnit tells you what happened — including the actual expression you wrote:

```
Expected to be 80
but found 100

at Assert.That(total).IsEqualTo(expected)
```

Comparing objects? Instead of dumping two object graphs at you, TUnit pinpoints the difference:

```
Expected to be equal to Employee { FirstName = "Victoria", LastName = "Apanii", Age = 30 }
but differs at member FirstName: expected "Victoria" but found "ictoria"

at Assert.That(actualEmployee).IsEqualTo(expectedEmployee)
```

## Why TUnit?

- **Compile-time test discovery** — tests are wired up by a source generator at build time, not found via reflection at runtime. Faster startup, better IDE integration, and full Native AOT / trimming support.
- **Compile-time safety** — a suite of Roslyn analyzers ships in the box, so mistakes like invalid hook signatures, broken data sources, and misused assertions fail your *build*, not your CI run.
- **Parallel by default, with real control** — tests run concurrently out of the box; `[DependsOn]`, `[NotInParallel]`, and `[ParallelLimiter<T>]` give you precise ordering and throttling when you need it.
- **Batteries included** — rich async assertions, shared fixtures with dependency injection, lifecycle hooks at every scope, and a source-generated mocking library — with first-class integrations for ASP.NET Core, Aspire, and Playwright.

## Performance

Source generation shifts work from run time to build time: you pay a little up front at build, and every test run after that starts faster — dramatically so under Native AOT. The same test suites, run on every framework:

<!-- benchmarks:start -->
| Scenario | TUnit (AOT) | TUnit | xUnit v3 | NUnit | MSTest |
|----------|---|---|---|---|---|
| Data-driven tests | 16.65 ms | 265.70 ms | 523.90 ms | 523.01 ms | 529.13 ms |
| Async-heavy tests | 113.1 ms | 360.0 ms | 605.6 ms | 608.6 ms | 676.0 ms |
| Matrix combinations | 119.6 ms | 474.1 ms | 1,469.2 ms | 1,468.9 ms | 1,515.3 ms |
| Large suites (scale) | 29.38 ms | 263.55 ms | 504.14 ms | 488.30 ms | 495.94 ms |
| Massive parallelism | 217.6 ms | 467.8 ms | 2,945.7 ms | 1,107.5 ms | 2,993.9 ms |
| Setup/teardown lifecycle | — | 335.8 ms | 1,050.2 ms | 1,008.8 ms | 1,099.9 ms |

<sub>Mean wall-clock time to run the same test suite. TUnit (AOT) 1.58.0 · TUnit 1.58.0 · xUnit v3 3.2.2 · NUnit 4.6.1 · MSTest 4.3.0. .NET SDK 10.0.301, .NET 10.0.9 (10.0.9, 10.0.926.27113), X64 RyuJIT x86-64-v4. Updated 2026-07-12 — regenerated weekly by the [Speed Comparison workflow](https://github.com/thomhurst/TUnit/actions/workflows/speed-comparison.yml). Full results and methodology: [tunit.dev/docs/benchmarks](https://tunit.dev/docs/benchmarks/).</sub>
<!-- benchmarks:end -->

## Getting Started

### Using the Project Template (Recommended)

```bash
dotnet new install TUnit.Templates
dotnet new TUnit -n "MyTestProject"
cd MyTestProject
dotnet run
```

### Manual Installation

```bash
dotnet add package TUnit
```

[Getting Started Guide](https://tunit.dev/docs/getting-started/installation) · [Migration Guides](https://tunit.dev/docs/migration/xunit)

## A tour of the good parts

### Data-driven tests

```csharp
[Test]
[Arguments("user1@test.com", "ValidPassword123")]
[Arguments("admin@test.com", "AdminPass789")]
public async Task User_Login_Succeeds(string email, string password) { ... }

// Matrix — generates a test for every combination (9 total here)
[Test]
[MatrixDataSource]
public async Task Database_Operations_Work(
    [Matrix("Create", "Update", "Delete")] string operation,
    [Matrix("User", "Product", "Order")] string entity) { ... }
```

Need more? `[MethodDataSource]` pulls rows from a method, and custom `DataSourceGenerator<T>` attributes let you build your own sources.

### Assertions that explain themselves

Assertions are async, chainable, and produce the focused failure messages shown above:

```csharp
await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK)
    .Because("the health endpoint should always be up");

await Assert.That(order.Items)
    .Count().IsEqualTo(3)
    .And.Contains(item => item.Sku == "ABC-123");
```

Defining your own assertion is one attribute on a plain method — TUnit generates the fluent extension for you:

```csharp
[GenerateAssertion]
public static bool IsPositive(this int value) => value > 0;

// Now available on Assert.That:
await Assert.That(account.Balance).IsPositive();
```

### Shared fixtures without the ceremony

Inject anything into your test classes with `[ClassDataSource<T>]`. Implement `IAsyncInitializer` for async setup, `IAsyncDisposable` for teardown, and pick a sharing scope — `None`, `PerClass`, `PerAssembly`, `PerTestSession`, or `Keyed`:

```csharp
public class PostgresContainer : IAsyncInitializer, IAsyncDisposable
{
    public Task InitializeAsync() { /* start container */ }
    public ValueTask DisposeAsync() { /* stop container */ }
}

[ClassDataSource<PostgresContainer>(Shared = SharedType.PerTestSession)]
public class OrderRepositoryTests(PostgresContainer postgres)
{
    [Test]
    public async Task Saves_Order() { /* postgres is initialized and shared across the whole run */ }
}
```

Property injection works too, and disposal is reference-counted — shared fixtures are torn down exactly when the last test using them finishes.

### Parallelism you control

Everything runs in parallel by default. Opt out or sequence tests where it matters:

```csharp
[Test]
public async Task Register_User() { ... }

[Test, DependsOn(nameof(Register_User))]
[Retry(3)]
public async Task Login_With_Registered_User() { ... } // runs after Register_User passes

[Test, NotInParallel("checkout-db")] // tests sharing a key never overlap
public async Task Migrates_Schema() { ... }
```

`[Repeat(n)]`, `[Timeout(ms)]`, and `[ParallelLimiter<T>]` round out the set.

### Lifecycle hooks at every scope

```csharp
[Before(Test)]        // also: Class, Assembly, TestSession
public async Task SetUp() { ... }

[After(Class)]
public static async Task TearDownDatabase(ClassHookContext context) { ... }
```

### Mocking built in

`TUnit.Mocks` is a source-generated, Native AOT-compatible mocking library — no runtime proxies, no `Castle.Core`. It works with any test framework:

```csharp
var gateway = IPaymentGateway.Mock();   // or Mock.Of<IPaymentGateway>()

gateway.ChargeAsync(Any<decimal>()).Returns(new ChargeResult(Success: true));

var checkout = new CheckoutService(gateway.Object);
await checkout.CompleteAsync(cart);

gateway.ChargeAsync(99.99m).WasCalled(Times.Once);
```

Companion packages mock the annoying stuff for you:

```csharp
// TUnit.Mocks.Http — a real HttpClient backed by a scriptable handler
using var client = Mock.HttpClient("https://api.example.com");
client.Handler.OnGet("/users/1").RespondWithJson("""{ "id": 1 }""");

// TUnit.Mocks.Logging — capture and verify ILogger output
var logger = Mock.Logger<CheckoutService>();
logger.VerifyLog().AtLevel(LogLevel.Warning).ContainingMessage("retrying").WasCalled(Times.Once);
```

### Custom attributes

Extend built-in base classes to create your own skip conditions, retry logic, and more:

```csharp
public class WindowsOnlyAttribute : SkipAttribute
{
    public WindowsOnlyAttribute() : base("Windows only") { }

    public override Task<bool> ShouldSkip(TestContext testContext)
        => Task.FromResult(!OperatingSystem.IsWindows());
}

[Test, WindowsOnly]
public async Task Windows_Specific_Feature() { ... }
```

## Integrations

### ASP.NET Core

```csharp
public class ApiFactory : TestWebApplicationFactory<Program>;

[ClassDataSource<ApiFactory>(Shared = SharedType.PerTestSession)]
public class HealthCheckTests(ApiFactory factory)
{
    [Test]
    public async Task Health_Endpoint_Responds()
    {
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/health");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }
}
```

### Aspire

Spin up your whole distributed app once per test session, with resource log forwarding and OpenTelemetry capture built in:

```csharp
public class AppFixture : AspireFixture<Projects.MyApp_AppHost>;

[ClassDataSource<AppFixture>(Shared = SharedType.PerTestSession)]
public class ApiServiceTests(AppFixture app)
{
    [Test]
    public async Task Api_Returns_Data()
    {
        var client = app.CreateHttpClient("apiservice");
        var response = await client.GetAsync("/weather");

        await Assert.That(response.IsSuccessStatusCode).IsTrue();
    }
}
```

### Playwright

Inherit from `PageTest` and a browser page is waiting for you — lifecycle fully managed:

```csharp
public class HomePageTests : PageTest
{
    [Test]
    public async Task Homepage_Loads()
    {
        await Page.GotoAsync("https://example.com");

        await Assert.That(await Page.TitleAsync()).Contains("Example");
    }
}
```

### Property-based testing (FsCheck)

```csharp
[Test, FsCheckProperty]
public bool Reversing_Twice_Returns_Original(int[] array) =>
    array.SequenceEqual(array.AsEnumerable().Reverse().Reverse());
```

## More than C#

TUnit runs F# and VB.NET test projects too, and `TUnit.Assertions.FSharp` provides idiomatic F# assertion helpers.

## IDE Support

| IDE | Notes |
|-----|-------|
| **Visual Studio 2022 (17.13+)** | Works out of the box |
| **Visual Studio 2022 (earlier)** | Enable "Use testing platform server mode" in Tools > Manage Preview Features |
| **JetBrains Rider** | Enable "Testing Platform support" in Settings > Build, Execution, Deployment > Unit Testing > Testing Platform |
| **VS Code** | Install [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) and enable "Use Testing Platform Protocol" |
| **CLI** | Works with `dotnet test`, `dotnet run`, and direct execution |

## Packages

| Package | Purpose |
|---------|---------|
| `TUnit` | Start here — the full framework (Core + Engine + Assertions) |
| `TUnit.Core` | Shared test library components without an execution engine |
| `TUnit.Engine` | Execution engine for test projects |
| `TUnit.Assertions` | Standalone assertions — works with other test frameworks too |
| `TUnit.Assertions.Should` | Optional FluentAssertions-style `value.Should().BeEqualTo(...)` syntax over `TUnit.Assertions` (beta) |
| `TUnit.Mocks` | Source-generated, AOT-compatible mocking — works with any test runner |
| `TUnit.Mocks.Http` | `HttpClient` mocking helpers built on `TUnit.Mocks` |
| `TUnit.Mocks.Logging` | `ILogger` capture/verification helpers built on `TUnit.Mocks` |
| `TUnit.AspNetCore` | ASP.NET Core integration — `WebApplicationFactory`-based test fixtures |
| `TUnit.Aspire` | Aspire integration — distributed app host fixtures with OpenTelemetry capture |
| `TUnit.Playwright` | Playwright integration with automatic browser lifecycle management |
| `TUnit.FsCheck` | Property-based testing via FsCheck |
| `TUnit.OpenTelemetry` | OpenTelemetry instrumentation for test runs |

## Migrating from xUnit, NUnit, or MSTest?

The syntax will feel familiar. For example, xUnit's `[Fact]` becomes `[Test]`, and `[Theory]` + `[InlineData]` becomes `[Test]` + `[Arguments]`. See the migration guides for full details: [xUnit](https://tunit.dev/docs/migration/xunit) · [NUnit](https://tunit.dev/docs/migration/nunit) · [MSTest](https://tunit.dev/docs/migration/mstest).

## Community

- [Documentation](https://tunit.dev) — guides, tutorials, and API reference
- [GitHub Discussions](https://github.com/thomhurst/TUnit/discussions) — questions and ideas welcome
- [Issues](https://github.com/thomhurst/TUnit/issues) — bug reports and feature requests
- [Changelog](https://github.com/thomhurst/TUnit/releases)
