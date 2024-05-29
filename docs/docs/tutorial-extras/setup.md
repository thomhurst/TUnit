---
sidebar_position: 1
---

# Test Set Ups

Most setup for a test can be performed in the constructor (think setting up mocks, assigning fields.)

However some scenarios require further setup that could be an asynchronous operation.
E.g. pinging a service to wake it up in preparation for the tests.

For this, we can declare a method with a `[BeforeEachTest]` or an `[BeforeAllTestsInClass]` attribute.

- `[BeforeEachTest]` methods should NOT be static, and they will be executed repeatedly before each test in their class starts.
- `[BeforeAllTestsInClass]` methods SHOULD be static, and they will be executed only once, before any test in their class starts.

Methods will be executed bottom-up, so the base class set ups will execute first and then the inheriting class.

```csharp
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass
{
    private int _value;
    private static HttpResponseMessage? _pingResponse;

    [BeforeAllTestsInClass]
    public static async Task Ping()
    {
        _pingResponse = await new HttpClient().GetAsync("https://localhost/ping");
    }
    
    [BeforeEachTest]
    public async Task Setup()
    {
        await Task.CompletedTask;
        
        _value = 99;
    }

    [Test]
    public async Task Test()
    {
        await Assert.That(_value).Is.EqualTo(99);
        await Assert.That(_pingResponse?.StatusCode).Is.Not.Null().And.Is.EqualTo(HttpStatusCode.OK);
    }
}
```