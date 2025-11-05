# TUnit Philosophy & Design Decisions

If you're wondering why TUnit does things differently from other testing frameworks, this page has the answers. Understanding the reasoning behind TUnit's design will help you use it effectively and decide if it's right for your project.

## Core Principles

### Performance First

TUnit is built for speed at any scale. Whether you have 100 tests or 100,000, they should run as fast as possible. Tests run in parallel by default, using Roslyn source generators to discover tests at compile time instead of expensive runtime reflection. You can choose between source-generated mode (fastest) or reflection mode (more flexible) depending on your needs.

Fast tests create faster feedback loops. When tests run quickly, developers actually run them more often. They catch bugs earlier and stay in flow instead of context-switching while waiting for test results.

### Modern .NET First

TUnit embraces modern .NET without compromises. Everything is async by default. Assertions, hooks, all of it. It uses C# 12+ features like collection expressions and file-scoped namespaces. Native AOT and trimming work out of the box. It's built on Microsoft.Testing.Platform instead of the legacy VSTest infrastructure.

Modern .NET applications deserve a modern testing framework. TUnit doesn't carry the baggage of .NET Framework support or patterns from a decade ago.

### Test Isolation

Every test should be completely independent. TUnit creates a new instance of your test class for each test method, so instance fields can't leak between tests. Tests can run in any order, on any thread, without affecting each other. If you need shared state, you make it explicit with the `static` keyword.

Isolated tests are reliable tests. You never get those mysterious failures where Test B only fails when Test A runs first. Everything is deterministic.

### Developer Experience

Writing tests should be pleasant, not painful. TUnit has minimal boilerplate—just put `[Test]` on your methods, no class attributes needed. Assertions read naturally: `await Assert.That(value).IsEqualTo(expected)`. Error messages are clear and actionable. You can access test metadata through `TestContext` whenever you need it.

Developers spend a lot of time writing and debugging tests. Small improvements in ergonomics really do add up over time.

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

Yes, it's more verbose. Yes, there's a learning curve. But it enables patterns that just aren't possible with sync assertions:

```csharp
// Custom async assertion
await Assert.That(async () => await GetUserAsync(id))
    .ThrowsAsync<UserNotFoundException>();

// Chained assertions without blocking
await Assert.That(user.Email)
    .IsNotNull()
    .And.Contains("@example.com");
```

The benefits outweigh the extra `await` keyword. Plus, the code fixers handle most of the migration work automatically anyway.

### Why Microsoft.Testing.Platform?

TUnit is built on Microsoft.Testing.Platform instead of the legacy VSTest infrastructure.

The new platform was designed for .NET 5+ from scratch. It's faster, more extensible, and both `dotnet test` and `dotnet run` work well with it. More importantly, it's where Microsoft is investing going forward.

The downside? Some older tools only work with VSTest. Coverlet is the most notable example. But Microsoft provides `Microsoft.Testing.Extensions.CodeCoverage` as the modern alternative, and it actually works better with the new platform anyway.

### Why Parallel by Default?

Most testing frameworks make you opt-in to parallelism. TUnit flips that around.

Parallel tests are dramatically faster. Modern CPUs have many cores—TUnit uses them. And here's the thing: tests that are safe to run in parallel are usually well-isolated tests. Making parallelism the default pushes you toward better test design.

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

Coming from NUnit (which shares instances by default)? Yes, this is a breaking change. But it's the right default for test isolation.

### Why Source Generators?

TUnit uses Roslyn source generators for test discovery. No runtime reflection means better performance and Native AOT compatibility. You get compile-time errors for test configuration issues instead of runtime surprises. IDEs understand the generated code, so IntelliSense and refactoring work better.

Source generators do add complexity and can make debugging trickier. But for most users, the performance and AOT benefits are worth it. If you really need more flexibility, reflection mode is always available with `dotnet test -- --reflection`.

## What Problems Does TUnit Solve?

### Slow Test Suites

Traditional frameworks run tests sequentially by default and use runtime reflection for discovery. TUnit runs tests in parallel and discovers them at compile time with source generators. The result? Test suites often run 5-10x faster.

### Flaky Tests from State Leaks

NUnit shares test class instances. xUnit allows state in constructors. It's easy to accidentally share state and get those frustrating "works alone, fails in the suite" bugs. TUnit creates a new instance per test with no way to opt out. If you need shared state, you use `static`, making it explicit. Parallel execution catches state problems early instead of hiding them.

### Limited Async Support

Older frameworks have a mix of sync and async APIs. You need `IAsyncLifetime` for async setup. Some parts do sync-over-async. TUnit is async everywhere—hooks, assertions, all of it. No deadlocks, clean code throughout.

### Poor AOT Support

Heavy runtime reflection doesn't work with Native AOT. TUnit uses source generation, supports AOT from day one, and has proper trimming annotations. Your tests work with Native AOT deployments.

## Comparison with Other Frameworks

### TUnit vs xUnit

xUnit and TUnit have a lot in common—neither requires class attributes, both have modern extensible designs. The main differences: TUnit runs parallel by default with better async control, uses source generation for discovery, has a richer hook system instead of constructor/IDisposable patterns, and uses fluent assertions instead of static methods.

### TUnit vs NUnit

Both use `[Test]` attributes and have rich assertion libraries. The biggest difference is isolation: TUnit creates a new instance per test, NUnit shares by default. TUnit also defaults to parallel execution (NUnit defaults to sequential), has async assertions (NUnit is sync), and doesn't need the `[TestFixture]` attribute.

### TUnit vs MSTest

Both are Microsoft-backed with good IDE integration. TUnit drops the class attribute requirement (MSTest needs `[TestClass]`), runs on the modern testing platform, defaults to parallel execution, and has better async support throughout.

For detailed comparisons, check out [Framework Differences](../comparison/framework-differences.md).

## When to Choose TUnit

TUnit is a good fit when performance matters—you have large test suites that need to run fast. It's designed for modern .NET (8+), works great with Native AOT, and shines when you want parallel test execution. If you're starting a new project without legacy constraints, TUnit is worth considering.

When might you want alternatives? If you're on .NET Framework (TUnit requires .NET 8+), use NUnit or xUnit. If you have an existing huge test suite, migration costs might outweigh the benefits. If your team strongly prefers another framework's style, that's a legitimate reason to stick with what works for you. Or if you absolutely need a tool that only works with VSTest, you'll need to use something else.

## The Bottom Line

TUnit exists because modern .NET deserves a modern testing framework. One that prioritizes performance, isolation, and developer experience without carrying the baggage of legacy compromises.

Every decision—async assertions, parallel-by-default, source generation—flows from wanting tests to be fast, isolated, modern, and pleasant to write. Tests should run in parallel, create new instances per test, support async naturally, and minimize boilerplate.

If that resonates with you, TUnit is probably a good fit for your project.

For migration details, check out:
- [xUnit Migration](../migration/xunit.md)
- [NUnit Migration](../migration/nunit.md)
- [MSTest Migration](../migration/mstest.md)
