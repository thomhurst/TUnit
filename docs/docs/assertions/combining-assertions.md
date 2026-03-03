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

`.And` and `.Or` can be mixed in a single chain. Conditions are evaluated left-to-right.

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
