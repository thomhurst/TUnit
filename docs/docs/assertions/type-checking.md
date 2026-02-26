---
sidebar_position: 5
---

# Type Checking

TUnit assertions check types at compile time wherever possible. This gives faster feedback and catches mistakes before your build pipeline runs.

So this wouldn't compile, because we're comparing an `int` and a `string`:

```csharp
    [Test]
    public async Task MyTest()
    {
        await Assert.That(1).IsEqualTo("1");
    }
```