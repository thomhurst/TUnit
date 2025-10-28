---
sidebar_position: 3
---

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

    public override Task<bool> ShouldRetry(TestContext context, Exception exception, int currentRetryCount)
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

## Global Retry Policy

In case you want to apply the retry logic to all tests in a project, you can add the attribute on the assembly level.

```csharp
[assembly: Retry(3)]
```

Or you can apply the retry policy on all the tests in a class like this:

```csharp
[Retry(3)]
public class MyTestClass
{
}
```

The more specific attribute will always override the more general one.
For example, the `[Retry(3)]` on a method will override the `[Retry(5)]` on the class,
which in turn will override the `[Retry(7)]` on the assembly.

So the order of precedence is:
1. Method
1. Class
1. Assembly
