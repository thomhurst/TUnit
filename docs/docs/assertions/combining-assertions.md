---
sidebar_position: 11
---

# Combining Assertions

TUnit provides several ways to combine multiple assertions within a single test: chaining with `.And` and `.Or`, and grouping with `Assert.Multiple()`.

## And Conditions

Use the `.And` property to chain multiple conditions on the same value. Every condition must pass for the assertion to succeed. This reads naturally and avoids repeating `Assert.That(...)` for each check.

```csharp
[Test]
public async Task MyTest()
{
    var result = Add(1, 2);

    await Assert.That(result)
        .IsNotNull()
        .And.IsPositive()
        .And.IsEqualTo(3);
}
```

## Or Conditions

Use the `.Or` property when at least one condition must pass. This is useful for values that are valid across a known set of outcomes.

```csharp
[Test]
public async Task MyTest()
{
    var result = ComputeValue();

    await Assert.That(result)
        .IsEqualTo(2)
        .Or.IsEqualTo(3)
        .Or.IsEqualTo(4);
}
```

:::warning Mixing And/Or is not supported
`.And` and `.Or` cannot be mixed in a single chain. Attempting to use `.Or` after `.And` (or vice versa) throws `MixedAndOrAssertionsException` at runtime. If you need both kinds of logic, split the chain across multiple `Assert.That(...)` calls, or combine the conditions into a single boolean expression beforehand.

```csharp
// NOT supported - throws MixedAndOrAssertionsException at runtime
await Assert.That(result)
    .IsPositive()
    .And.IsLessThan(10)
    .Or.IsEqualTo(100);

// Supported - use separate assertions
await Assert.That(result).IsPositive().And.IsLessThan(10);
// or fall back to a single boolean check when you need And/Or together
await Assert.That((result > 0 && result < 10) || result == 100).IsTrue();
```
:::

## Assertion Scopes

By default, a failing assertion throws immediately and stops the test. `Assert.Multiple()` creates a scope that collects all failures and reports them together when the scope exits. This is useful for asserting multiple properties on an object without the fix-one-rerun-fix-another cycle.

Implicit scope (covers the rest of the method):

```csharp
[Test]
public async Task MyTest()
{
    var result = Add(1, 2);

    using var _ = Assert.Multiple();

    await Assert.That(result).IsPositive();
    await Assert.That(result).IsEqualTo(3);
}
```

Explicit scope (covers only the block):

```csharp
[Test]
public async Task MyTest()
{
    var result = Add(1, 2);

    using (Assert.Multiple())
    {
        await Assert.That(result).IsPositive();
        await Assert.That(result).IsEqualTo(3);
    }
}
```

Both forms aggregate failures. When the `using` scope ends, any accumulated assertion failures are thrown as a single exception listing all violations.

## See Also

- [Getting Started with Assertions](getting-started.md) — Assertion basics and fluent syntax
- [Exception Assertions](exceptions.md) — Assert on thrown exceptions
