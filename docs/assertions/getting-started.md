# Getting Started with Assertions

TUnit provides a comprehensive, fluent assertion library that makes your tests readable and expressive. This guide introduces the core concepts and gets you started with writing assertions.

## Basic Syntax[​](#basic-syntax "Direct link to Basic Syntax")

All assertions in TUnit follow a consistent pattern using the `Assert.That()` method:

```
await Assert.That(actualValue).IsEqualTo(expectedValue);
```

The basic flow is:

1. Start with `Assert.That(value)`
2. Chain assertion methods (e.g., `.IsEqualTo()`, `.Contains()`, `.IsGreaterThan()`)
3. Always `await` the assertion (TUnit's assertions are async)

## Why Await?[​](#why-await "Direct link to Why Await?")

TUnit assertions must be awaited — they won't execute without `await`, and the test will pass silently:

```
// ✅ Correct - awaited

await Assert.That(result).IsEqualTo(42);



// ❌ Wrong - assertion never runs, test passes without checking

Assert.That(result).IsEqualTo(42);
```

A built-in analyzer warns if you forget. See [Awaiting Assertions](/docs/assertions/awaiting.md) for more examples and design rationale.

> Prefer `value.Should().BeEqualTo(...)`? Install the optional [`TUnit.Assertions.Should`](/docs/assertions/should-syntax.md) package — it layers a FluentAssertions-style entry surface on top of the same assertion infrastructure. Both syntaxes coexist in the same project.

## Assertion Categories[​](#assertion-categories "Direct link to Assertion Categories")

TUnit provides assertions for all common scenarios:

### Equality & Comparison[​](#equality--comparison "Direct link to Equality & Comparison")

```
await Assert.That(actual).IsEqualTo(expected);

await Assert.That(value).IsNotEqualTo(other);

await Assert.That(score).IsGreaterThan(70);

await Assert.That(age).IsLessThanOrEqualTo(100);

await Assert.That(temperature).IsBetween(20, 30);
```

### Strings[​](#strings "Direct link to Strings")

```
await Assert.That(message).Contains("Hello");

await Assert.That(filename).StartsWith("test_");

await Assert.That(email).Matches(@"^[\w\.-]+@[\w\.-]+\.\w+$");

await Assert.That(input).IsNotEmpty();
```

### Collections[​](#collections "Direct link to Collections")

```
await Assert.That(numbers).Contains(42);

await Assert.That(items).Count().IsEqualTo(5);

await Assert.That(list).IsNotEmpty();

await Assert.That(values).All(x => x > 0);
```

### Booleans & Null[​](#booleans--null "Direct link to Booleans & Null")

```
await Assert.That(isValid).IsTrue();

await Assert.That(obj).IsNotNull();

await Assert.That(optional).IsDefault();
```

### Exceptions[​](#exceptions "Direct link to Exceptions")

```
await Assert.That(() => DivideByZero())

    .Throws<DivideByZeroException>()

    .WithMessage("Attempted to divide by zero.");
```

### Type Checking[​](#type-checking "Direct link to Type Checking")

```
await Assert.That(obj).IsTypeOf<MyClass>();

await Assert.That(typeof(Dog)).IsAssignableTo<Animal>();
```

## Chaining Assertions[​](#chaining-assertions "Direct link to Chaining Assertions")

Combine multiple assertions on the same value using `.And`:

```
await Assert.That(username).IsNotNull().And.IsNotEmpty();

await Assert.That(username).Length().IsGreaterThan(3);

await Assert.That(username).Length().IsLessThan(20);
```

Use `.Or` when any condition can be true:

```
await Assert.That(statusCode)

    .IsEqualTo(200)

    .Or.IsEqualTo(201)

    .Or.IsEqualTo(204);
```

## Multiple Assertions with Assert.Multiple()[​](#multiple-assertions-with-assertmultiple "Direct link to Multiple Assertions with Assert.Multiple()")

Group related assertions together so all failures are reported:

```
using (Assert.Multiple())

{

    await Assert.That(user.FirstName).IsEqualTo("John");

    await Assert.That(user.LastName).IsEqualTo("Doe");

    await Assert.That(user.Age).IsGreaterThan(18);

    await Assert.That(user.Email).IsNotNull();

}
```

Instead of stopping at the first failure, `Assert.Multiple()` runs all assertions and reports every failure together.

## Member Assertions[​](#member-assertions "Direct link to Member Assertions")

Assert on object properties using `.Member()`:

```
await Assert.That(person)

    .Member(p => p.Name, name => name.IsEqualTo("Alice"))

    .And.Member(p => p.Age, age => age.IsGreaterThan(18));
```

This works with nested properties too:

```
await Assert.That(order)

    .Member(o => o.Customer.Address.City, city => city.IsEqualTo("Seattle"));
```

## Working with Collections[​](#working-with-collections "Direct link to Working with Collections")

Collections have rich assertion support:

```
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

## Returning Values from Assertions[​](#returning-values-from-assertions "Direct link to Returning Values from Assertions")

Some assertions return the value being tested, allowing you to continue working with it:

```
// HasSingleItem returns the single item

var user = await Assert.That(users).HasSingleItem();

await Assert.That(user.Name).IsEqualTo("Alice");



// Contains with predicate returns the found item

var admin = await Assert.That(users).Contains(u => u.Role == "Admin");

await Assert.That(admin.Permissions).IsNotEmpty();
```

## Custom Expectations[​](#custom-expectations "Direct link to Custom Expectations")

Use `.Satisfies()` for custom conditions:

```
await Assert.That(value).Satisfies(v => v % 2 == 0, "Value must be even");
```

Or map to a different value before asserting:

```
await Assert.That(order)

    .Member(o => o.Total, total => total.IsGreaterThan(100));
```

## Common Patterns[​](#common-patterns "Direct link to Common Patterns")

### Testing Numeric Ranges[​](#testing-numeric-ranges "Direct link to Testing Numeric Ranges")

```
await Assert.That(score).IsBetween(0, 100);

await Assert.That(temperature).IsGreaterThanOrEqualTo(32);
```

### Testing with Tolerance[​](#testing-with-tolerance "Direct link to Testing with Tolerance")

For floating-point comparisons:

```
await Assert.That(3.14159).IsEqualTo(Math.PI).Within(0.001);
```

### Testing Async Operations[​](#testing-async-operations "Direct link to Testing Async Operations")

```
await Assert.That(async () => await FetchDataAsync())

    .Throws<HttpRequestException>();



await Assert.That(longRunningTask).CompletesWithin(TimeSpan.FromSeconds(5));
```

### Testing Multiple Conditions[​](#testing-multiple-conditions "Direct link to Testing Multiple Conditions")

```
await Assert.That(username)

    .IsNotNull()

    .And.Satisfies(name => name!.Length >= 3 && name.Length <= 20,

                   "Username must be 3-20 characters");
```

## Type Safety[​](#type-safety "Direct link to Type Safety")

TUnit's assertions are strongly typed and catch type mismatches at compile time:

```
int number = 42;

string text = "42";

_ = text;



// ✅ This works - both are ints

await Assert.That(number).IsEqualTo(42);



// ❌ This won't compile - can't compare int to string

// await Assert.That(number).IsEqualTo("42");
```

## Common Mistakes[​](#common-mistakes "Direct link to Common Mistakes")

* **Forgetting `await`** — Unawaited assertions never execute; the test passes silently. Always `await Assert.That(...)`. The compiler warns about this, but it's the most common TUnit mistake. See [Awaiting Assertions](/docs/assertions/awaiting.md).
* **Type confusion** — `Assert.That(number).IsEqualTo("42")` won't compile. TUnit assertions are strongly typed. Convert explicitly before asserting.
* **Assuming collection order** — Use `IsEquivalentTo()` instead of `IsEqualTo()` when item order doesn't matter (e.g., database results).
* **Sequential assertions hiding failures** — Wrap related assertions in `using (Assert.Multiple()) { ... }` to see all failures at once instead of stopping at the first.

## Next Steps[​](#next-steps "Direct link to Next Steps")

Now that you understand the basics, explore specific assertion types:

* **[Equality & Comparison](/docs/assertions/equality-and-comparison.md)** - Detailed equality and comparison assertions
* **[Strings](/docs/assertions/string.md)** - Comprehensive string testing
* **[Collections](/docs/assertions/collections.md)** - Advanced collection assertions
* **[Exceptions](/docs/assertions/exceptions.md)** - Testing thrown exceptions
* **[Custom Assertions](/docs/assertions/extensibility/custom-assertions.md)** - Create your own assertions

## Quick Reference[​](#quick-reference "Direct link to Quick Reference")

| Assertion Category | Example                                          |
| ------------------ | ------------------------------------------------ |
| Equality           | `IsEqualTo()`, `IsNotEqualTo()`                  |
| Comparison         | `IsGreaterThan()`, `IsLessThan()`, `IsBetween()` |
| Null/Default       | `IsNull()`, `IsNotNull()`, `IsDefault()`         |
| Boolean            | `IsTrue()`, `IsFalse()`                          |
| Strings            | `Contains()`, `StartsWith()`, `Matches()`        |
| Collections        | `Contains()`, `Count()`, `All()`, `Any()`        |
| Exceptions         | `Throws<T>()`, `ThrowsNothing()`                 |
| Types              | `IsTypeOf<T>()`, `IsAssignableTo<T>()`           |
| Async              | `CompletesWithin()`, async exception testing     |

For a complete list of all assertions, see the specific category pages in the sidebar.
