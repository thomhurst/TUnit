---
sidebar_position: 11
---

# Task and Async Assertions

TUnit provides specialized assertions for testing `Task` and `Task<T>` objects, including state checking, completion timeouts, and async exception handling.

## Task State Assertions

### IsCompleted / IsNotCompleted

Tests whether a task has completed (successfully, faulted, or canceled):

```csharp
[Test]
public async Task Task_Is_Completed()
{
    var completedTask = Task.CompletedTask;
    await Assert.That(completedTask).IsCompleted();

    var runningTask = Task.Delay(10000);
    await Assert.That(runningTask).IsNotCompleted();
}
```

### IsCanceled / IsNotCanceled

Tests whether a task was canceled:

```csharp
[Test]
public async Task Task_Is_Canceled()
{
    var cts = new CancellationTokenSource();
    cts.Cancel();

    var task = Task.Run(() => { }, cts.Token);

    try
    {
        await task;
    }
    catch (TaskCanceledException)
    {
        // Expected
    }

    await Assert.That(task).IsCanceled();
}
```

```csharp
[Test]
public async Task Task_Not_Canceled()
{
    var task = Task.CompletedTask;

    await Assert.That(task).IsNotCanceled();
}
```

### IsFaulted / IsNotFaulted

Tests whether a task ended in a faulted state (threw an exception):

```csharp
[Test]
public async Task Task_Is_Faulted()
{
    var faultedTask = Task.Run(() => throw new InvalidOperationException());

    try
    {
        await faultedTask;
    }
    catch
    {
        // Expected
    }

    await Assert.That(faultedTask).IsFaulted();
}
```

```csharp
[Test]
public async Task Task_Not_Faulted()
{
    var successfulTask = Task.CompletedTask;

    await Assert.That(successfulTask).IsNotFaulted();
}
```

### IsCompletedSuccessfully / IsNotCompletedSuccessfully (.NET 6+)

Tests whether a task completed successfully (not faulted or canceled):

```csharp
[Test]
public async Task Task_Completed_Successfully()
{
    var task = Task.CompletedTask;

    await Assert.That(task).IsCompletedSuccessfully();
}
```

```csharp
[Test]
public async Task Task_Not_Completed_Successfully()
{
    var cts = new CancellationTokenSource();
    cts.Cancel();
    var canceledTask = Task.FromCanceled(cts.Token);

    await Assert.That(canceledTask).IsNotCompletedSuccessfully();
}
```

## Timeout Assertions

### CompletesWithin

Tests that a task completes within a specified time:

```csharp
[Test]
public async Task Task_Completes_Within_Timeout()
{
    var fastTask = Task.Delay(100);

    await Assert.That(fastTask).CompletesWithin(TimeSpan.FromSeconds(1));
}
```

Fails if timeout exceeded:

```csharp
[Test]
public async Task Task_Exceeds_Timeout()
{
    var slowTask = Task.Delay(5000);

    // This will fail - task takes longer than timeout
    // await Assert.That(slowTask).CompletesWithin(TimeSpan.FromMilliseconds(100));
}
```

### WaitsFor

Waits for a condition to become true within a timeout:

```csharp
[Test]
public async Task Wait_For_Condition()
{
    bool condition = false;

    _ = Task.Run(async () =>
    {
        await Task.Delay(500);
        condition = true;
    });

    await Assert.That(() => condition)
        .WaitsFor(c => c == true, timeout: TimeSpan.FromSeconds(2));
}
```

## Practical Examples

### API Call Timeout

```csharp
[Test]
public async Task API_Call_Completes_In_Time()
{
    var apiTask = _httpClient.GetAsync("https://api.example.com/data");

    await Assert.That(apiTask).CompletesWithin(TimeSpan.FromSeconds(5));

    var response = await apiTask;
    await Assert.That(response.IsSuccessStatusCode).IsTrue();
}
```

### Background Task Completion

```csharp
[Test]
public async Task Background_Processing_Completes()
{
    var processingTask = ProcessDataInBackgroundAsync();

    await Assert.That(processingTask).CompletesWithin(TimeSpan.FromMinutes(1));
    await Assert.That(processingTask).IsCompletedSuccessfully();
}
```

### Cancellation Token Handling

```csharp
[Test]
public async Task Operation_Respects_Cancellation()
{
    using var cts = new CancellationTokenSource();
    cts.CancelAfter(TimeSpan.FromMilliseconds(100));

    var task = LongRunningOperationAsync(cts.Token);

    try
    {
        await task;
    }
    catch (OperationCanceledException)
    {
        // Expected
    }

    await Assert.That(task).IsCanceled();
}
```

### Async Exception Handling

For testing exceptions in async code, use exception assertions:

```csharp
[Test]
public async Task Async_Method_Throws_Exception()
{
    await Assert.That(async () => await FailingOperationAsync())
        .Throws<InvalidOperationException>();
}
```

### Task Result Assertions

For `Task<T>`, await the task first, then assert on the result:

```csharp
[Test]
public async Task Task_Returns_Expected_Result()
{
    var task = GetValueAsync();

    // Ensure it completes in time
    await Assert.That(task).CompletesWithin(TimeSpan.FromSeconds(1));

    // Get the result
    var result = await task;

    // Assert on the result
    await Assert.That(result).IsEqualTo(42);
}
```

### Parallel Task Execution

```csharp
[Test]
public async Task Parallel_Tasks_Complete()
{
    var task1 = Task.Delay(100);
    var task2 = Task.Delay(100);
    var task3 = Task.Delay(100);

    var allTasks = Task.WhenAll(task1, task2, task3);

    await Assert.That(allTasks).CompletesWithin(TimeSpan.FromSeconds(1));
    await Assert.That(allTasks).IsCompletedSuccessfully();
}
```

### Task State Transitions

```csharp
[Test]
public async Task Task_State_Progression()
{
    var tcs = new TaskCompletionSource<int>();
    var task = tcs.Task;

    // Initially not completed
    await Assert.That(task).IsNotCompleted();

    // Complete the task
    tcs.SetResult(42);

    // Now completed
    await Assert.That(task).IsCompleted();
    await Assert.That(task).IsCompletedSuccessfully();

    var result = await task;
    await Assert.That(result).IsEqualTo(42);
}
```

### Failed Task

```csharp
[Test]
public async Task Task_Fails_With_Exception()
{
    var tcs = new TaskCompletionSource<int>();
    var task = tcs.Task;

    tcs.SetException(new InvalidOperationException("Operation failed"));

    await Assert.That(task).IsFaulted();
    await Assert.That(task).IsNotCompletedSuccessfully();
}
```

### Canceled Task

```csharp
[Test]
public async Task Task_Can_Be_Canceled()
{
    var tcs = new TaskCompletionSource<int>();
    var task = tcs.Task;

    tcs.SetCanceled();

    await Assert.That(task).IsCanceled();
    await Assert.That(task).IsNotCompletedSuccessfully();
}
```

## WhenAll and WhenAny

### WhenAll Completion

```csharp
[Test]
public async Task All_Tasks_Complete()
{
    var tasks = Enumerable.Range(1, 5)
        .Select(i => Task.Delay(i * 100))
        .ToArray();

    var allCompleted = Task.WhenAll(tasks);

    await Assert.That(allCompleted).CompletesWithin(TimeSpan.FromSeconds(1));
}
```

### WhenAny Completion

```csharp
[Test]
public async Task Any_Task_Completes()
{
    var fastTask = Task.Delay(100);
    var slowTask = Task.Delay(5000);

    var firstCompleted = Task.WhenAny(fastTask, slowTask);

    await Assert.That(firstCompleted).CompletesWithin(TimeSpan.FromMilliseconds(500));

    var completed = await firstCompleted;
    await Assert.That(completed).IsSameReferenceAs(fastTask);
}
```

## ValueTask Assertions

`ValueTask` and `ValueTask<T>` work similarly:

```csharp
[Test]
public async Task ValueTask_Completion()
{
    var valueTask = GetValueTaskAsync();

    var result = await valueTask;
    await Assert.That(result).IsGreaterThan(0);
}

async ValueTask<int> GetValueTaskAsync()
{
    await Task.Delay(10);
    return 42;
}
```

## Chaining Task Assertions

```csharp
[Test]
public async Task Chained_Task_Assertions()
{
    var task = GetDataAsync();

    await Assert.That(task)
        .CompletesWithin(TimeSpan.FromSeconds(5));

    await Assert.That(task)
        .IsCompleted()
        .And.IsCompletedSuccessfully()
        .And.IsNotCanceled()
        .And.IsNotFaulted();
}
```

## Common Patterns

### Retry Logic Testing

```csharp
[Test]
public async Task Retry_Eventually_Succeeds()
{
    int attempts = 0;

    var task = RetryAsync(async () =>
    {
        attempts++;
        if (attempts < 3)
            throw new Exception("Temporary failure");
        return "Success";
    }, maxRetries: 5);

    await Assert.That(task).CompletesWithin(TimeSpan.FromSeconds(10));
    var result = await task;
    await Assert.That(result).IsEqualTo("Success");
}
```

### Debounce Testing

```csharp
[Test]
public async Task Debounced_Operation()
{
    var trigger = new Subject<string>();
    var debouncedTask = trigger
        .Throttle(TimeSpan.FromMilliseconds(500))
        .FirstAsync()
        .ToTask();

    trigger.OnNext("value");

    await Assert.That(debouncedTask)
        .CompletesWithin(TimeSpan.FromSeconds(1));
}
```

### Circuit Breaker Testing

```csharp
[Test]
public async Task Circuit_Breaker_Opens()
{
    var circuitBreaker = new CircuitBreaker();

    // Fail enough times to open circuit
    for (int i = 0; i < 5; i++)
    {
        try
        {
            await circuitBreaker.ExecuteAsync(() => throw new Exception());
        }
        catch { }
    }

    // Circuit should be open
    var task = circuitBreaker.ExecuteAsync(() => Task.CompletedTask);

    await Assert.That(async () => await task)
        .Throws<CircuitBreakerOpenException>();
}
```

### Producer-Consumer Testing

```csharp
[Test]
public async Task Producer_Consumer_Processes_Items()
{
    var channel = Channel.CreateUnbounded<int>();

    var producer = ProduceItemsAsync(channel.Writer);
    var consumer = ConsumeItemsAsync(channel.Reader);

    await Assert.That(producer).CompletesWithin(TimeSpan.FromSeconds(1));
    await Assert.That(consumer).CompletesWithin(TimeSpan.FromSeconds(2));
}
```

### Rate Limiting

```csharp
[Test]
public async Task Rate_Limiter_Delays_Requests()
{
    var rateLimiter = new RateLimiter(maxRequests: 5, perTimeSpan: TimeSpan.FromSeconds(1));

    var stopwatch = Stopwatch.StartNew();

    // Make 10 requests (should take ~2 seconds due to rate limiting)
    var tasks = Enumerable.Range(0, 10)
        .Select(_ => rateLimiter.ExecuteAsync(() => Task.CompletedTask));

    await Task.WhenAll(tasks);
    stopwatch.Stop();

    await Assert.That(stopwatch.Elapsed).IsGreaterThan(TimeSpan.FromSeconds(1.5));
}
```

## Testing Async Disposal

```csharp
[Test]
public async Task Async_Disposable_Cleanup()
{
    var resource = new AsyncResource();

    await using (resource)
    {
        // Use resource
    }

    // After disposal
    await Assert.That(resource.IsDisposed).IsTrue();
}
```

## See Also

- [Exceptions](exceptions.md) - Testing async exceptions
- [DateTime](datetime.md) - Timeout and duration testing
- [Boolean](boolean.md) - Testing task state booleans
