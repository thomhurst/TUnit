---
sidebar_position: 3
---

# Assertion Scopes

In TUnit you can create an assertion scope by calling `Assert.Multiple()`.
This returns an `IAsyncDisposable` and so you should use that by encapsulating the returned value in an `await using` block. This will make sure that the assertions are executed only after the scope is exited.

If you use an assertion scope, then any assertions that are performed inside of it will not throw an exception immediately. Instead, they will lazily execute their logic, and then aggregate any exceptions together at the end of the scope and throw them together. 

This is useful for asserting multiple properties and showing all errors at once, instead of having to fix > rerun > fix > rerun.

```csharp
    [Test]
    public async Task MyTest()
    {
        var result = Add(1, 2);

        await using var _ = Assert.Multiple();

        await Assert.That(result).IsPositive();
        await Assert.That(result).IsEqualTo(3);
    }
```