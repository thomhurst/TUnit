# Failure Signals

Use a failure signal when a callback, event handler, or detached operation discovers a failure outside the test's awaited call stack. It safely transfers that failure to TUnit instead of throwing on the callback thread.

For failures raised directly by the test's awaited code, continue to use `Fail.Test(...)`.

## Basic Usage

Create the signal before the test body starts, subscribe to the external callback, and unsubscribe during cleanup:

```csharp
public class ConnectionTests : ITestStartEventReceiver, ITestEndEventReceiver
{
    private ITestFailureSignal _failureSignal = null!;
    private Action<Exception>? _failureHandler;

    public ValueTask OnTestStart(TestContext context)
    {
        _failureSignal = context.Execution.CreateFailureSignal();
        _failureHandler = exception => _failureSignal.Report(exception);
        Connection.FatalError += _failureHandler;

        return default;
    }

    public ValueTask OnTestEnd(TestContext context)
    {
        Connection.FatalError -= _failureHandler;
        return default;
    }

    [Test]
    public async Task ConnectionRemainsHealthy(CancellationToken cancellationToken)
    {
        await Connection.RunAsync(cancellationToken);
    }
}
```

`CreateFailureSignal()` must be called from a `[Before(Test)]` hook or `ITestStartEventReceiver.OnTestStart`. Calling it after the test body has started throws an `InvalidOperationException`. Repeated calls during the same attempt return the same signal.

Capture the signal in the callback as shown above. A callback running on another thread should not depend on `TestContext.Current` being available.

## Reporting Failures

Report either the original exception or a reason:

```csharp
failureSignal.Report(exception);
failureSignal.Report("The background service stopped unexpectedly");
```

`Report` does not throw the supplied failure on the callback thread. The first report wins; later or post-test reports are ignored.

Use `TryReport` when the callback needs to know whether TUnit accepted its report:

```csharp
if (!failureSignal.TryReport(exception))
{
    // Another callback already reported a failure, or the test has ended.
}
```

## Cancellation and Cleanup

When a failure is reported, TUnit cancels the test's execution token and waits for a bounded grace period before continuing to teardown. Accept and observe the injected `CancellationToken` so the test body can stop promptly:

```csharp
[Test]
public async Task ProcessMessages(CancellationToken cancellationToken)
{
    await processor.RunAsync(cancellationToken);
}
```

Always unsubscribe callbacks in an `[After(Test)]` hook or `ITestEndEventReceiver.OnTestEnd`. A failure signal is scoped to one test attempt, so retries receive a fresh signal and reports from an earlier attempt are ignored.

## Custom Test Executors

Failure signals compose with `ITestExecutor`, test timeouts, and tokens added through `AddLinkedCancellationToken`. The `action` receives the latest linked execution token. A custom executor should await the action and place cleanup in `finally` so it completes during signal cancellation:

```csharp
public sealed class CustomExecutor : ITestExecutor
{
    public async ValueTask ExecuteTest(TestContext context, Func<ValueTask> action)
    {
        using var cancellationSource = new CancellationTokenSource();
        context.Execution.AddLinkedCancellationToken(cancellationSource.Token);

        try
        {
            await action();
        }
        finally
        {
            await ReleaseExecutorResourcesAsync();
        }
    }
}
```

Create the failure signal in a test-start receiver or before-test hook, not inside `ITestExecutor.ExecuteTest`, because the test body execution phase has already begun when the executor is invoked.

If both the signal and the custom executor report exceptions while stopping, TUnit preserves both failures.

## Performance

Failure signals are opt-in. When `CreateFailureSignal()` is not called, TUnit does not allocate the signal's cancellation source or completion task and does not race the test body against a signal task. The normal execution path only performs a nullable check.
