# Retrying

Unfortunately sometimes our tests hit issues. It could be a blip on the network, but that could cause our entire test suite to fail which is frustrating.

If you want to retry a test, add a `[RetryAttribute]` onto your test method or class. This takes an `int` of how many times you'd like to retry.

This can be used on base classes and inherited to affect all tests in sub-classes.

```csharp
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass
{
    [Test]
    [Retry(3)]
    public async Task MyTest()
    {
        
    }
}
```

The basic `RetryAttribute` will retry on any exception.

If you only want to retry on certain conditions, you can create your own attribute that inherits from `RetryAttribute`. There's a `ShouldRetry` method that can be overridden, where you can access the test information and the type of exception that was thrown.

If this method returns true, the test can be retried, otherwise it'll fail.

```csharp
using TUnit.Core;

namespace MyTestProject;

public class RetryTransientHttpAttribute : RetryAttribute
{
    public RetryTransientHttpAttribute(int times) : base(times)
    {
    }

    public override Task<bool> ShouldRetry(TestInformation testInformation, Exception exception)
    {
        if (exception is HttpRequestException requestException)
        {
            return Task.FromResult(requestException.StatusCode is
                HttpStatusCode.BadGateway
                or HttpStatusCode.TooManyRequests
                or HttpStatusCode.GatewayTimeout
                or HttpStatusCode.RequestTimeout);
        }

        return Task.FromResult(false);
    }
}

public class MyTestClass
{
    [Test]
    [RetryTransientHttp(3)]
    public async Task MyTest()
    {
        
    }
}
```
