---
sidebar_position: 3
---

# Assertion Scopes

In TUnit you can create an assertion scope by calling `Assert.Multiple()`.
This returns an `IAsyncDisposable` and so should call both `await` and `using`. This will make sure that the assertions are executed when the scope is exited.

If you use an assertion scope, then any assertions that are performed inside of it will not throw an exception immediately. Instead, they will lazily execute their logic, and then aggregate any exceptions together at the end of the scope and throw them together. 

This is useful for asserting multiple properties and showing all errors at once, instead of having to fix > rerun > fix > rerun.

```csharp
    [Test]
    public async Task MyTest()
    {
        var result = Add(1, 2);

        await Assert.Multiple(() =>
        {
            Assert.That(result).IsPositive();
            Assert.That(result).IsEqualTo(3);
        });
    }
```