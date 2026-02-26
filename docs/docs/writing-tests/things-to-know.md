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

import Tabs from '@theme/Tabs';
import TabItem from '@theme/TabItem';

<Tabs>
  <TabItem value="bad" label="❌ Bad - Will Fail" default>

```csharp
public class MyTests
{
    private int _value;

    [Test, NotInParallel]
    public void MyTest1() { _value = 99; }

    [Test, NotInParallel]
    public async Task MyTest2()
    {
        // This will FAIL because _value is 0
        // Different test instance = different _value
        await Assert.That(_value).IsEqualTo(99);
    }
}
```

**Why this fails:** Each test gets a new instance of `MyTests`, so `_value` in `MyTest2` is a different field than in `MyTest1`.

  </TabItem>
  <TabItem value="good" label="✅ Good - Use Static">

```csharp
public class MyTests
{
    private static int _value;

    [Test, NotInParallel]
    public void MyTest1() { _value = 99; }

    [Test, NotInParallel]
    public async Task MyTest2()
    {
        // This works because _value is static
        await Assert.That(_value).IsEqualTo(99);
    }
}
```

**Why this works:** The `static` keyword makes the field persist across instances, making it clear that data is shared.

  </TabItem>
</Tabs>
