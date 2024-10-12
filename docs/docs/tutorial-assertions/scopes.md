---
sidebar_position: 3
---

# Assertion Scopes

In TUnit you can create an assertion scope by calling `Assert.Multiple()`. This returns an `IDisposable` and so you should use that by encapsulating the returned value in a `using` block. This will make sure that any assertion exceptionss are aggregated together and thrown only after the scope is exited.

This is useful for asserting multiple properties and showing all errors at once, instead of having to fix > rerun > fix > rerun.

Implicit Scope:

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

Explicit Scope:

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
