# Things to know

TUnit has made some decisions by design. You may need to know about them:

## Parallelisation

Tests are run in parallel by design. If you have operations you can't do in parallel, you'll need to add a `[NotInParallel]` attribute to your test. That attribute can also take an `Order` property, so you can control the ordering of your not in parallel tests.

## Test Classes and Instance Data

Classes are `new`ed up for each test within their class. 

This is by design because tests should be stateless and side effect free. 

By doing this it enables parallelisation (for speed and throughput), and reduces bugs and side effects when there is stale data left over from previous tests. Test suites can appear green while actually being broken, because they assert against instance data left over from previous tests.

So if you have:

```csharp
public class MyTests
{
    [Test]
    public void MyTest1() { ... }

    [Test]
    public void MyTest2() { ... }
}
```

Then `MyTest1` and `MyTest2` will have a different instance of `MyTests`.

This isn't that important unless you're storing state.

```csharp
public class MyTests
{
    private int _value;  // ❌ reset to 0 for every test — different instances!

    [Test, NotInParallel]
    public void MyTest1() { _value = 99; }

    [Test, NotInParallel]
    public async Task MyTest2()
    {
        // FAILS — _value is 0 because this is a different instance
        await Assert.That(_value).IsEqualTo(99);
    }
}
```

**Fix:** Use `private static int _value;` if you genuinely need shared state, but prefer making tests independent or using `[ClassDataSource<>]` instead.

## See Also

- [Test Lifecycle](lifecycle.md) — Understand the order of setup, execution, and cleanup
- [Hooks](hooks.md) — Run code before and after tests, classes, or assemblies
- [Controlling Parallelism](../execution/parallelism.md) — Configure how tests run in parallel
