# Things to know

TUnit has made some decisions by design. You may need to know about them:

## Parallelisation

Tests are run in parallel by design. If you have operations you can't do in parallel, you'll need to add a `[NotInParallel]` attribute to your test. That attribute can also take an `Order` property, so you can control the ordering of your not in parallel tests.

## Test Classes and Instance Data

Classes are `new`ed up for each test within their class. 

This is by design because tests should be stateless and side effect free. 

By doing this it enables parallelisation (for speed and throughput), and reduces bugs and side effects when there is stale data left over from previous tests. This is something I've experienced with NUnit before. I've seen test suites that were all green, and they were actually broken, because they were asserting against instance data that had been left over from previous tests.

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

So you can't do this:

```csharp
public class MyTests
{
    private int _value;

    [Test, NotInParallel]
    public void MyTest1() { _value = 99; }

    [Test, NotInParallel]
    public async Task MyTest2() { await Assert.That(_value).IsEqualTo(99); }
}
```

The above will compile fine and run, but it will result in a failing test.

Because `MyTests` in `MyTest2` is different from `MyTests` in `MyTest1`, therefore the `_value` field is a different reference.

If you really want to perform a test like the above, you can make your field static, and then that field will persist across any instance. The `static` keyword makes it clear to the user that data persists outside of instances.

```csharp
public class MyTests
{
    private static int _value;

    [Test, NotInParallel]
    public void MyTest1() { _value = 99; }

    [Test, NotInParallel]
    public async Task MyTest2() { await Assert.That(_value).IsEqualTo(99); }
}
```
