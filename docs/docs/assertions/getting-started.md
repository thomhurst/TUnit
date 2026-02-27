---
sidebar_position: 1
---

# Getting Started with Assertions

TUnit provides a comprehensive, fluent assertion library that makes your tests readable and expressive. This guide introduces the core concepts and gets you started with writing assertions.

## Basic Syntax

All assertions in TUnit follow a consistent pattern using the `Assert.That()` method:

```csharp
await Assert.That(actualValue).IsEqualTo(expectedValue);
```

The basic flow is:
1. Start with `Assert.That(value)`
2. Chain assertion methods (e.g., `.IsEqualTo()`, `.Contains()`, `.IsGreaterThan()`)
3. Always `await` the assertion (TUnit's assertions are async)

## Why Await?

TUnit assertions must be awaited — they won't execute without `await`, and the test will pass silently:

```csharp
// ✅ Correct - awaited
await Assert.That(result).IsEqualTo(42);

// ❌ Wrong - assertion never runs, test passes without checking
Assert.That(result).IsEqualTo(42);
```

A built-in analyzer warns if you forget. See [Awaiting Assertions](awaiting.md) for more examples and design rationale.

## Assertion Categories

TUnit provides assertions for all common scenarios:

### Equality & Comparison

```csharp
await Assert.That(actual).IsEqualTo(expected);
await Assert.That(value).IsNotEqualTo(other);
await Assert.That(score).IsGreaterThan(70);
await Assert.That(age).IsLessThanOrEqualTo(100);
await Assert.That(temperature).IsBetween(20, 30);
```

### Strings

```csharp
await Assert.That(message).Contains("Hello");
await Assert.That(filename).StartsWith("test_");
await Assert.That(email).Matches(@"^[\w\.-]+@[\w\.-]+\.\w+$");
await Assert.That(input).IsNotEmpty();
```

### Collections

```csharp
await Assert.That(numbers).Contains(42);
await Assert.That(items).Count().IsEqualTo(5);
await Assert.That(list).IsNotEmpty();
await Assert.That(values).All(x => x > 0);
```

### Booleans & Null

```csharp
await Assert.That(isValid).IsTrue();
await Assert.That(result).IsNotNull();
await Assert.That(optional).IsDefault();
```

### Exceptions

```csharp
await Assert.That(() => DivideByZero())
    .Throws<DivideByZeroException>()
    .WithMessage("Attempted to divide by zero.");
```

### Type Checking

```csharp
await Assert.That(obj).IsTypeOf<MyClass>();
await Assert.That(typeof(Dog)).IsAssignableTo<Animal>();
```

## Chaining Assertions

Combine multiple assertions on the same value using `.And`:

```csharp
await Assert.That(username)
    .IsNotNull()
    .And.IsNotEmpty()
    .And.Length().IsGreaterThan(3)
    .And.Length().IsLessThan(20);
```

Use `.Or` when any condition can be true:

```csharp
await Assert.That(statusCode)
    .IsEqualTo(200)
    .Or.IsEqualTo(201)
    .Or.IsEqualTo(204);
```

## Multiple Assertions with Assert.Multiple()

Group related assertions together so all failures are reported:

```csharp
using (Assert.Multiple())
{
    await Assert.That(user.FirstName).IsEqualTo("John");
    await Assert.That(user.LastName).IsEqualTo("Doe");
    await Assert.That(user.Age).IsGreaterThan(18);
    await Assert.That(user.Email).IsNotNull();
}
```

Instead of stopping at the first failure, `Assert.Multiple()` runs all assertions and reports every failure together.

## Member Assertions

Assert on object properties using `.Member()`:

```csharp
await Assert.That(person)
    .Member(p => p.Name, name => name.IsEqualTo("Alice"))
    .And.Member(p => p.Age, age => age.IsGreaterThan(18));
```

This works with nested properties too:

```csharp
await Assert.That(order)
    .Member(o => o.Customer.Address.City, city => city.IsEqualTo("Seattle"));
```

## Working with Collections

Collections have rich assertion support:

```csharp
var numbers = new[] { 1, 2, 3, 4, 5 };

// Count and emptiness
await Assert.That(numbers).Count().IsEqualTo(5);
await Assert.That(numbers).IsNotEmpty();

// Membership
await Assert.That(numbers).Contains(3);
await Assert.That(numbers).DoesNotContain(10);

// Predicates
await Assert.That(numbers).All(n => n > 0);
await Assert.That(numbers).Any(n => n == 3);

// Ordering
await Assert.That(numbers).IsInOrder();

// Equivalence (same items, any order)
await Assert.That(numbers).IsEquivalentTo(new[] { 5, 4, 3, 2, 1 });
```

## Returning Values from Assertions

Some assertions return the value being tested, allowing you to continue working with it:

```csharp
// HasSingleItem returns the single item
var user = await Assert.That(users).HasSingleItem();
await Assert.That(user.Name).IsEqualTo("Alice");

// Contains with predicate returns the found item
var admin = await Assert.That(users).Contains(u => u.Role == "Admin");
await Assert.That(admin.Permissions).IsNotEmpty();
```

## Custom Expectations

Use `.Satisfies()` for custom conditions:

```csharp
await Assert.That(value).Satisfies(v => v % 2 == 0, "Value must be even");
```

Or map to a different value before asserting:

```csharp
await Assert.That(order)
    .Satisfies(o => o.Total, total => total > 100);
```

## Common Patterns

### Testing Numeric Ranges

```csharp
await Assert.That(score).IsBetween(0, 100);
await Assert.That(temperature).IsGreaterThanOrEqualTo(32);
```

### Testing with Tolerance

For floating-point comparisons:

```csharp
await Assert.That(3.14159).IsEqualTo(Math.PI).Within(0.001);
```

### Testing Async Operations

```csharp
await Assert.That(async () => await FetchDataAsync())
    .Throws<HttpRequestException>();

await Assert.That(longRunningTask).CompletesWithin(TimeSpan.FromSeconds(5));
```

### Testing Multiple Conditions

```csharp
await Assert.That(username)
    .IsNotNull()
    .And.Satisfies(name => name.Length >= 3 && name.Length <= 20,
                   "Username must be 3-20 characters");
```

## Type Safety

TUnit's assertions are strongly typed and catch type mismatches at compile time:

```csharp
int number = 42;
string text = "42";

// ✅ This works - both are ints
await Assert.That(number).IsEqualTo(42);

// ❌ This won't compile - can't compare int to string
// await Assert.That(number).IsEqualTo("42");
```

## Common Mistakes

- **Forgetting `await`** — Unawaited assertions never execute; the test passes silently. Always `await Assert.That(...)`. The compiler warns about this, but it's the most common TUnit mistake. See [Awaiting Assertions](awaiting.md).
- **Type confusion** — `Assert.That(number).IsEqualTo("42")` won't compile. TUnit assertions are strongly typed. Convert explicitly before asserting.
- **Assuming collection order** — Use `IsEquivalentTo()` instead of `IsEqualTo()` when item order doesn't matter (e.g., database results).
- **Sequential assertions hiding failures** — Wrap related assertions in `using (Assert.Multiple()) { ... }` to see all failures at once instead of stopping at the first.

## Next Steps

Now that you understand the basics, explore specific assertion types:

- **[Equality & Comparison](equality-and-comparison.md)** - Detailed equality and comparison assertions
- **[Strings](string.md)** - Comprehensive string testing
- **[Collections](collections.md)** - Advanced collection assertions
- **[Exceptions](exceptions.md)** - Testing thrown exceptions
- **[Custom Assertions](extensibility/custom-assertions.md)** - Create your own assertions

## Quick Reference

| Assertion Category | Example |
|-------------------|---------|
| Equality | `IsEqualTo()`, `IsNotEqualTo()` |
| Comparison | `IsGreaterThan()`, `IsLessThan()`, `IsBetween()` |
| Null/Default | `IsNull()`, `IsNotNull()`, `IsDefault()` |
| Boolean | `IsTrue()`, `IsFalse()` |
| Strings | `Contains()`, `StartsWith()`, `Matches()` |
| Collections | `Contains()`, `Count()`, `All()`, `Any()` |
| Exceptions | `Throws<T>()`, `ThrowsNothing()` |
| Types | `IsTypeOf<T>()`, `IsAssignableTo<T>()` |
| Async | `CompletesWithin()`, async exception testing |

For a complete list of all assertions, see the specific category pages in the sidebar.
