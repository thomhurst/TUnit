---
sidebar_position: 1.5
title: Should Syntax (Optional)
description: FluentAssertions-style value.Should().BeEqualTo() syntax via the optional TUnit.Assertions.Should NuGet package.
---

# Should Syntax

`TUnit.Assertions.Should` is an **optional** add-on package that exposes a FluentAssertions-style entry surface — `value.Should().BeEqualTo(...)` — on top of `TUnit.Assertions`. It's a thin layer: every Should-flavored method is generated from an existing `TUnit.Assertions` assertion, so behaviour, error messages, and async semantics are identical.

This page is for users coming from FluentAssertions / Shouldly who prefer the `Should()` cadence. The default `Assert.That(...)` syntax remains the canonical TUnit style — the two are interchangeable and don't compete.

## Installation

```bash
dotnet add package TUnit.Assertions.Should --prerelease
```

The package is published in **beta** — versions are stamped `{semver}-beta` until the API stabilises.

It depends on `TUnit.Assertions` directly. You can keep `Assert.That(...)` and `value.Should()` in the same project; the two share their underlying assertion infrastructure (context, expression builder, async pipeline) so chains and failure messages compose cleanly.

## Quick Comparison

| `Assert.That` style | `Should()` style |
|---|---|
| `await Assert.That(value).IsEqualTo(5)` | `await value.Should().BeEqualTo(5)` |
| `await Assert.That(value).IsNotNull()` | `await value.Should().NotBeNull()` |
| `await Assert.That(text).Contains("foo")` | `await text.Should().Contain("foo")` |
| `await Assert.That(text).StartsWith("hi")` | `await text.Should().StartWith("hi")` |
| `await Assert.That(list).Contains(item)` | `await list.Should().Contain(item)` |
| `await Assert.That(list).IsInOrder()` | `await list.Should().BeInOrder()` |
| `await Assert.That(list).All(p)` | `await list.Should().All(p)` |
| `await Assert.That(() => Foo()).Throws<E>()` | `await ((Action)(() => Foo())).Should().Throw<E>()` |

## Naming Rules

The package's source generator scans every `[AssertionExtension]`, `[GenerateAssertion]`, and `[AssertionFrom<T>]` declaration in the referenced `TUnit.Assertions` assembly and emits a Should-flavored counterpart with a conjugated method name:

| Original | Should-flavored | Rule |
|---|---|---|
| `Is*` | `Be*` | `IsEqualTo` → `BeEqualTo`, `IsZero` → `BeZero` |
| `IsNot*` | `NotBe*` | `IsNotNull` → `NotBeNull`, `IsNotEqualTo` → `NotBeEqualTo` |
| `Has*` | `Have*` | `HasCount` → `HaveCount`, `HasFiles` → `HaveFiles` |
| `DoesNot*` | `Not*` | `DoesNotContain` → `NotContain`, `DoesNotMatch` → `NotMatch` |
| `Does*` | (strip prefix) | `DoesMatch` → `Match` |
| 3rd-person singular `*s` | (drop trailing `-s`) | `Contains` → `Contain`, `StartsWith` → `StartWith`, `Throws` → `Throw` |

Boundary-aware: `Issue` doesn't become `Besue`. The first word of the method name is matched against the rule, not the substring.

### Custom names

For irregulars or when the conjugation produces an unwanted name, decorate the assertion class with `[ShouldName("...")]`. The override is consulted before the conjugation rules:

```csharp
[AssertionExtension("IsOdd")]
[ShouldName("BeAnOddNumber")]
public class OddAssertion : Assertion<int> { … }
```

`[AssertionExtension(NegatedMethodName = "...")]` produces a second extension method for the negated form, which the Should generator picks up and conjugates independently — `Contains` → `Contain` and `DoesNotContain` → `NotContain` come out automatically without any `[ShouldName]`. When TUnit's pattern uses **separate classes** for positive and negated forms (e.g. `EqualsAssertion` + `NotEqualsAssertion`), place a separate `[ShouldName]` on each:

```csharp
[AssertionExtension("IsBetween")]
[ShouldName("BeWithinRange")]
public class BetweenAssertion<TValue> : Assertion<TValue> { … }

[AssertionExtension("IsNotBetween")]
[ShouldName("NotBeWithinRange")]
public class NotBetweenAssertion<TValue> : Assertion<TValue> { … }
```

## Entry Points

Each entry overload returns a wrapper appropriate to the source type:

```csharp
// Value entry — returns ShouldSource<T>
await 42.Should().BeEqualTo(42);
await "hello".Should().Contain("ell");
await someObject.Should().BeOfType<MyClass>();

// Collection entry — returns ShouldCollectionSource<TItem>
//   exposes element-typed instance methods (BeInOrder, All, Any,
//   HaveSingleItem, HaveDistinctItems) without explicit type arguments
var list = new List<int> { 1, 2, 3 };
await list.Should().BeInOrder();
await list.Should().All(x => x > 0);
await list.Should().Contain(2);

// Delegate entry — returns ShouldDelegateSource<T>
//   exposes Throw / ThrowExactly directly
Action act = () => throw new InvalidOperationException();
await act.Should().Throw<InvalidOperationException>();

Func<Task<int>> async = () => Task.FromResult(42);
await async.Should().BeEqualTo(42);
```

## Chaining

`.And` and `.Or` continuations stay Should-flavored end-to-end — the chain types only expose the Should naming, so you can't accidentally drop back to `Is*`/`Has*` mid-chain:

```csharp
await value
    .Should().BeEqualTo(5)
    .And.NotBeEqualTo(7)
    .And.BeBetween(1, 10);

await statusCode
    .Should().BeEqualTo(200)
    .Or.BeEqualTo(201)
    .Or.BeEqualTo(204);
```

Mixing `.And` and `.Or` without explicit grouping throws `MixedAndOrAssertionsException` at runtime — the analyzer flags it at compile time too.

## Assert.Multiple

Works unchanged. Should-flavored assertions share the underlying `AssertionScope`:

```csharp
using (Assert.Multiple())
{
    await user.FirstName.Should().BeEqualTo("Alice");
    await user.LastName.Should().BeEqualTo("Smith");
    await user.Age.Should().BeGreaterThan(18);
}
```

## Because

Add a justification message that appears in failure output:

```csharp
await score.Should().BeGreaterThan(70).Because("passing grade required");
```

## User-defined assertions

Any assertion you write with `[AssertionExtension]`, `[GenerateAssertion]`, or `[AssertionFrom<T>]` automatically gets a Should counterpart. No additional wiring needed.

```csharp
[AssertionExtension("IsOdd")]
public sealed class OddAssertion : Assertion<int>
{
    public OddAssertion(AssertionContext<int> context) : base(context) { }
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<int> metadata)
        => Task.FromResult(metadata.Value % 2 != 0
            ? AssertionResult.Passed
            : AssertionResult.Failed($"{metadata.Value} is even"));
    protected override string GetExpectation() => "to be odd";
}

// Usage:
await 3.Should().BeOdd();
```

## Analyzer support

The existing TUnit assertion analyzers also recognise the Should syntax:

- `TUnitAssertions0002` (assertion not awaited) fires for unawaited `value.Should().X()` chains.
- `TUnitAssertions0001` (mixing And/Or) fires for mixed Should chains.
- The nullability suppressor recognises `value.Should().NotBeNull()` and suppresses CS8600/CS8602/CS8604/CS8618/CS8629 on the asserted variable in subsequent statements.

All checks are scoped to TUnit namespaces — unrelated `Should()` extensions in other libraries (e.g. FluentAssertions, custom user code) don't trigger any of these diagnostics.

## FluentAssertions coexistence

Both libraries declare a `Should()` extension on `T` (and string, IEnumerable, etc.). If a project references both:

- An ambiguity (CS0121) at the `Should()` call site forces you to pick one.
- The conjugated names downstream are different (`BeEqualTo` vs FA's `Be`), so once you've passed the entry, the two surfaces don't collide.

For migration projects that want to flip incrementally, prefer one-file-at-a-time using directives or a global using alias to disambiguate the entry point.

## Limitations

- **Methods with method-level generic parameters** on collection wrappers (e.g. `IsAssignableTo<T>`, `IsTypeOf<T>` reached via `.Should()` on a collection) are not source-generated; use `Assert.That(value).IsAssignableTo<T>()` instead.
- **Cross-type extensions** (assertions whose source type differs from their assertion's value type, like `IsEqualTo` accepting an implicit-conversion target) skip the Should generation — use `Assert.That(...)` for those.
- **Three predicate-overload collection methods** (`All`, `Any`, `HaveSingleItem(predicate)`) are hand-written rather than generated because their target ctors require a literal-fallback string the simple-factory template can't supply.

These limitations don't apply to the underlying assertions — `Assert.That(...)` covers everything.
