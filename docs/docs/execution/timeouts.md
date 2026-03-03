---
sidebar_position: 5
---

# Timeouts

If you want to stop a test after a specified amount of time, add a `[Timeout]` attribute onto your test method or class. This takes an `int` of how many milliseconds a test can execute for.

When the timeout is exceeded, the test fails and the `CancellationToken` is cancelled. Any operations using the token will be aborted, preventing wasted system resources on a test that has already failed.

This can be used on base classes and inherited to affect all tests in sub-classes.

## Example

Pass the `CancellationToken` parameter to your test method to receive the timeout-linked token. Forward it to any async operations:

```csharp
using TUnit.Core;

namespace MyTestProject;

public class MyTestClass
{
    [Test]
    [Timeout(30_000)]
    public async Task Api_Responds_Within_30_Seconds(CancellationToken cancellationToken)
    {
        using var client = new HttpClient();

        var response = await client.GetAsync("https://api.example.com/health", cancellationToken);

        await Assert.That(response.IsSuccessStatusCode).IsTrue();
    }
}
```

If the HTTP call takes longer than 30 seconds, `cancellationToken` is cancelled, the `GetAsync` call throws an `OperationCanceledException`, and the test is reported as failed due to timeout.

## Timeout and Retries

When a test has both `[Timeout]` and `[Retry]`, each retry attempt gets its own fresh timeout. If the first attempt times out at 5 seconds, the retry starts from zero with a new 5-second window:

```csharp
[Test]
[Timeout(5_000)]
[Retry(2)]
public async Task Flaky_Service_Call(CancellationToken cancellationToken)
{
    var result = await FlakyService.CallAsync(cancellationToken);

    await Assert.That(result.Status).IsEqualTo("OK");
}
```

## Global Timeout

In case you want to apply the timeout to all tests in a project, you can add the attribute on the assembly level.

```csharp
[assembly: Timeout(30_000)]
```

Or you can apply the Timeout on all the tests in a class like this:

```csharp
[Timeout(30_000)]
public class MyTestClass
{
}
```

The more specific attribute will always override the more general one.
For example, the `[Timeout(3000)]` on a method will override the `[Timeout(5000)]` on the class,
which in turn will override the `[Timeout(7000)]` on the assembly.

So the order of precedence is:
1. Method
1. Class
1. Assembly
