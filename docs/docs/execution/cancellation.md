# Cancelling a Test

Call `TestContext.Execution.Cancel()` to request cooperative cancellation of the current test without affecting other tests or the test session:

```csharp
[Test]
public async Task ProcessMessages(CancellationToken cancellationToken)
{
    var execution = TestContext.Current!.Execution;

    messagePump.ShuttingDown += execution.Cancel;

    try
    {
        await messagePump.RunAsync(cancellationToken);
    }
    finally
    {
        messagePump.ShuttingDown -= execution.Cancel;
    }
}
```

TUnit marks the test as `Cancelled` and cancels both the injected `CancellationToken` and `TestContext.Execution.CancellationToken`. Cancellation is cooperative: the test body must observe the token and stop.

Capture `TestContext.Execution` for callbacks running on another thread. Do not depend on `TestContext.Current` being available in those callbacks.

`Cancel()` is thread-safe and may be called more than once. Calls made after test execution has completed are ignored. Cleanup hooks still run with `CancellationToken.None`, allowing them to finish normally.

Cancellation composes with `[Timeout]`, custom `ITestExecutor` implementations, and tokens added through `AddLinkedCancellationToken`.

TUnit creates one lightweight `CancellationTokenSource` for each executed test attempt. `Cancel()` adds no background task, polling, or `Task.WhenAny` work.
