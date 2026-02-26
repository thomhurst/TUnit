# TUnit Philosophy & Design Decisions

If you're wondering why TUnit does things differently from other testing frameworks, this page has the answers. Understanding the reasoning behind TUnit's design will help you use it effectively and decide if it's right for your project.

## Core Principles

### Performance First

TUnit is built for speed at any scale. Whether you have 100 tests or 100,000, they should run as fast as possible. Tests run in parallel by default, using Roslyn source generators to discover tests at compile time instead of expensive runtime reflection. You can choose between source-generated mode (fastest) or reflection mode (more flexible) depending on your needs.

### Modern .NET First

TUnit embraces modern .NET without compromises. Everything is async by default. Assertions, hooks, all of it. It uses C# 12+ features like collection expressions and file-scoped namespaces. Native AOT and trimming work out of the box. It's built on Microsoft.Testing.Platform instead of the legacy VSTest infrastructure.

TUnit supports .NET Standard 2.0 but is designed around modern patterns, not legacy approaches.

### Test Isolation

Every test should be completely independent. TUnit creates a new instance of your test class for each test method, so instance fields can't leak between tests. Tests can run in any order, on any thread, without affecting each other. If you need shared state, you make it explicit with the `static` keyword.

Isolated tests are reliable tests.

But what about when tests really do need to depend on each other? Use `[DependsOn]` to enforce ordering. It ensures the dependency always runs first, and if it fails, the dependent test gets skipped instead of running with bad state. This reduces flakiness compared to hoping tests run in the right order by accident.

### Developer Experience

Writing tests should be pleasant, not painful. TUnit has minimal boilerplate—just put `[Test]` on your methods, no class attributes needed. Assertions read naturally: `await Assert.That(value).IsEqualTo(expected)`. Error messages are clear and actionable. You can access test metadata through `TestContext` whenever you need it.

## Key Design Decisions

### Why Dual-Mode Execution?

TUnit offers two ways to discover and run tests: source generation (the default) and reflection mode.

Source-generated mode discovers tests at compile time using Roslyn source generators. It generates explicit test registration code, which makes it the fastest option. The downside is you need to recompile when tests change, but that's usually not a problem.

Reflection mode discovers tests at runtime. It's slightly slower but more flexible for dynamic scenarios. No code generation means it's simpler in some ways, but you lose the performance benefits.

Why support both? Different scenarios need different trade-offs. CI/CD pipelines benefit from maximum speed with source generation. AOT scenarios require it. But if you're doing something dynamic or just want the simplicity of runtime discovery, reflection mode is there. Users aren't locked into one approach.

### Why All Assertions Must Be Awaited

This is probably TUnit's most controversial decision: all assertions return `Task` and must be awaited.

```csharp
// TUnit - must await
await Assert.That(result).IsEqualTo(expected);

// Other frameworks - no await
Assert.Equal(expected, result);
```

The reasoning: consistency and extensibility. If all assertions work the same way, you never have to remember which ones need await and which don't. Custom assertions can do async work like database queries or HTTP calls. You can chain assertions naturally without blocking threads. And you avoid all the sync-over-async deadlock problems.

This enables patterns not possible with sync assertions:

```csharp
// Custom async assertion
await Assert.That(async () => await GetUserAsync(id))
    .ThrowsAsync<UserNotFoundException>();

// Chained assertions without blocking
await Assert.That(user.Email)
    .IsNotNull()
    .And.Contains("@example.com");
```

### Why Microsoft.Testing.Platform?

TUnit is built on Microsoft.Testing.Platform instead of the legacy VSTest infrastructure.

The new platform was designed for .NET 5+ from scratch. It's faster, more extensible, and both `dotnet test` and `dotnet run` work well with it. More importantly, it's where Microsoft is investing going forward.

The downside? Some older tools only work with VSTest. Coverlet is the most notable example. But Microsoft provides `Microsoft.Testing.Extensions.CodeCoverage` as the modern alternative, and it actually works better with the new platform anyway.

### Why Parallel by Default?

Most testing frameworks make you opt-in to parallelism. TUnit flips that around.

Running tests in parallel uses all CPU cores. Making parallelism the default encourages better test isolation.

When do you opt-out? Use `[NotInParallel]` for tests that modify shared files or databases, use global state, must run in a specific order, or access hardware like cameras or GPIO pins.

```csharp
[Test, NotInParallel]
public async Task ModifiesConfigFile()
{
    // This test modifies a shared config file
}
```

### Why New Instance Per Test?

TUnit creates a new instance of your test class for each test method.

```csharp
public class MyTests
{
    private int _counter = 0;  // Fresh for each test

    [Test]
    public async Task Test1()
    {
        _counter++;
        await Assert.That(_counter).IsEqualTo(1);  // Always passes
    }

    [Test]
    public async Task Test2()
    {
        _counter++;
        await Assert.That(_counter).IsEqualTo(1);  // Always passes
    }
}
```

This prevents instance fields from leaking between tests. No race conditions on instance data. No mysterious "Test B fails when Test A runs first" issues. If you need shared state, you use `static`, which makes it explicit and obvious in the code.

### Why Source Generators?

TUnit uses Roslyn source generators for test discovery. No runtime reflection means better performance and Native AOT compatibility. You get compile-time errors for test configuration issues instead of runtime surprises. IDEs understand the generated code, so IntelliSense and refactoring work better.

If you need more flexibility, reflection mode is available with `dotnet test -- --reflection`.

## What Problems Does TUnit Solve?

### Slow Test Suites

Traditional frameworks run tests sequentially by default and use runtime reflection for discovery. TUnit runs tests in parallel by default and discovers them at compile time with source generators.

### Flaky Tests from State Leaks

TUnit creates a new instance per test with no way to opt out. If you need shared state, you use `static`, making it explicit.

### Limited Async Support

TUnit is async everywhere—hooks, assertions, all of it. No deadlocks, clean code throughout.

### Poor AOT Support

TUnit uses source generation and has proper trimming annotations. Your tests work with Native AOT deployments.

## Comparison with Other Frameworks

For detailed comparisons, see [Framework Differences](../comparison/framework-differences.md).

## When to Choose TUnit

TUnit is a good fit when performance matters—you have large test suites that need to run fast. It supports .NET Standard 2.0, so it works with .NET Framework and all modern .NET versions. It works great with Native AOT, and shines when you want parallel test execution. If you're starting a new project without legacy constraints, TUnit is worth considering.

For migration details, check out:
- [xUnit Migration](../migration/xunit.md)
- [NUnit Migration](../migration/nunit.md)
- [MSTest Migration](../migration/mstest.md)
