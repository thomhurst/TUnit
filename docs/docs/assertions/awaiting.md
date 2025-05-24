# Awaiting

In TUnit you `await` your assertions, and this serves two purposes:
- the `await` keyword is responsible for performing the assertion, before you call await we're building a chain of assertion rules.
- it allows executing and asserting on `async` delegates without performing sync-over-async

Because of this, your tests should be `async` and return a `Task`.

Don't worry about forgetting to `await` - There's an analyzer built in that will notify you if you've missed any!

This will error:

```csharp
    [Test]
    public void MyTest()
    {
        var result = Add(1, 2);

        Assert.That(result).IsEqualTo(3);
    }
```

This won't: 

```csharp
    [Test]
    public async Task MyTest()
    {
        var result = Add(1, 2);

        await Assert.That(result).IsEqualTo(3);
    }
```

TUnit is able to take in asynchronous delegates. To be able to assert on these, we need to execute the code. We want to avoid sync-over-async, as this can cause problems and block the thread pool, slowing down your test suite.
And with how fast .NET has become, the overhead of `Task`s and `async` methods shouldn't be noticable.
