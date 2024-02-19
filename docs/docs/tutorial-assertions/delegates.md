---
sidebar_position: 6
---

# Delegates

TUnit can execute your delegates for you, and this allows you to assert on the data returned (if any was) - Or on any exceptions thrown:

```csharp
    [Test]
    public async Task MyTest()
    {
        await Assert.That(() =>
        {
            // Do something here
        }).Throws.Nothing();
    }

    // or

    [Test]
    public async Task MyTest()
    {
        await Assert.That(() =>
        {
            // Do something here
        }).Throws.Exception().OfType<ArgumentNullException>();
    }

    // or 

    [Test]
    public async Task MyTest()
    {
        var argumentNullException = await Assert.ThrowsAsync<ArgumentNullException>(() =>
        {
            // Do something here
            return Task.CompletedTask;
        });
    }
```