# Philosophy

TUnit does some things differently from other .NET testing frameworks. This page explains the thinking behind those choices.

## Parallel by default

Most frameworks make you opt into parallelism. TUnit flips that — tests run in parallel from the start, because that's how you get fast feedback from large test suites.

This also nudges you toward better test design. If your tests can't run in parallel, they're probably sharing state they shouldn't be. When they genuinely do need exclusive access to something (a shared file, a database, a hardware device), you opt out explicitly:

```csharp
[Test, NotInParallel]
public async Task ModifiesSharedConfigFile() { ... }
```

## New instance per test

Every test method runs against a fresh instance of its class. Instance fields can't leak between tests, and you'll never see the "Test B fails when Test A runs first" mystery.

If you need shared state, use `static`. That makes the sharing visible to anyone reading the code — no surprises.

## Async everywhere

All assertions return `Task` and must be awaited. This is probably TUnit's most controversial decision.

```csharp
await Assert.That(result).IsEqualTo(expected);
```

The reasoning: if everything is async, you never have to remember which operations need `await` and which don't. Custom assertions can do genuinely async work — database queries, HTTP calls — without awkward sync-over-async hacks. And you avoid the deadlock problems that come with mixing sync and async code.

## Source-generated test discovery

TUnit uses Roslyn source generators to discover tests at compile time rather than runtime reflection. This makes test discovery fast, gives you compile-time errors for configuration mistakes instead of runtime surprises, and is what makes Native AOT and single-file publishing work.

If you need runtime flexibility, reflection mode is available with `--reflection`.

## Built on Microsoft.Testing.Platform

TUnit uses Microsoft's modern testing platform rather than the legacy VSTest infrastructure. It's faster, more extensible, and where Microsoft is investing going forward.

The trade-off is that some older tools only work with VSTest — Coverlet being the most notable. But `Microsoft.Testing.Extensions.CodeCoverage` is the modern replacement and is included in the TUnit package automatically.

## Type-safe assertions

TUnit's assertions are extension methods on specific types, not generic methods that accept anything. Intellisense only shows assertions that make sense for what you're testing. You can't accidentally check if a string is negative, because that method doesn't exist on strings.

```csharp
await Assert.That(user.Email)
    .IsNotNull()
    .And.Contains("@example.com");
```

## Minimal boilerplate

Just put `[Test]` on a method. No `[TestClass]`, no `[TestFixture]`, no base classes required. Data attributes like `[Arguments]` and `[MethodDataSource]` work on both classes and methods. Global usings are configured automatically.

The goal is to spend your time writing tests, not scaffolding.

## Comparison with other frameworks

For specific comparisons with xUnit, NUnit, and MSTest, see [Framework Differences](../comparison/framework-differences.md).

For migration guides, see [xUnit](../migration/xunit.md), [NUnit](../migration/nunit.md), or [MSTest](../migration/mstest.md).
