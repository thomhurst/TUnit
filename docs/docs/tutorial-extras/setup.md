---
sidebar_position: 1
---

# Test Set Ups

Most setup for a test can be performed in the constructor (think setting up mocks, assigning fields.)

However some scenarios require further setup that could be an asynchronous operation.
E.g. pinging a service to wake it up in preparation for the tests.

For this, we can declare a method with a `[Before(...)]` or a `[GlobalBefore(...)]` attribute.

- `[Before(EachTest)]` methods should NOT be static, and they will be executed repeatedly before each test in their class starts.
- `[Before(Class)]` methods SHOULD be static, and they will be executed only once, before any test in their class starts.
- `[Before(Assembly)]` methods SHOULD be static, and they will be executed only once, before any test in their assembly starts.


- All `[GlobalBefore(...)]` methods SHOULD be static, and they will follow the same behaviour as above, but fire for every test/class/assembly that is being run in the test session.

Methods will be executed bottom-up, so the base class set ups will execute first and then the inheriting classes.

```csharp
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass
{
    private int _value;
    private static HttpResponseMessage? _pingResponse;

    [Before(Class)]
    public static async Task Ping()
    {
        _pingResponse = await new HttpClient().GetAsync("https://localhost/ping");
    }
    
    [Before(EachTest)]
    public async Task Setup()
    {
        await Task.CompletedTask;
        
        _value = 99;
    }

    [Test]
    public async Task Test()
    {
        await Assert.That(_value).IsEqualTo(99);
        await Assert.That(_pingResponse?.StatusCode).IsNot.Null().And.IsEqualTo(HttpStatusCode.OK);
    }
}
```