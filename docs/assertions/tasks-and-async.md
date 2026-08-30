# Task and Async Assertions

TUnit provides specialized assertions for testing `Task` and `Task<T>` objects, including state checking, completion timeouts, and async exception handling.

## Task State Assertions[​](#task-state-assertions "Direct link to Task State Assertions")

### IsCompleted / IsNotCompleted[​](#iscompleted--isnotcompleted "Direct link to IsCompleted / IsNotCompleted")

Tests whether a task has completed (successfully, faulted, or canceled):

```
[Test]

public async Task Task_Is_Completed()

{

    var completedTask = Task.CompletedTask;

    await Assert.That(completedTask).IsCompleted();



    var runningTask = Task.Delay(10000);

    await Assert.That(runningTask).IsNotCompleted();

}
```

### IsCanceled / IsNotCanceled[​](#iscanceled--isnotcanceled "Direct link to IsCanceled / IsNotCanceled")

Tests whether a task was canceled:

```
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

```
[Test]

public async Task Task_Not_Canceled()

{

    var task = Task.CompletedTask;



    await Assert.That(task).IsNotCanceled();

}
```

### IsFaulted / IsNotFaulted[​](#isfaulted--isnotfaulted "Direct link to IsFaulted / IsNotFaulted")

Tests whether a task ended in a faulted state (threw an exception):

```
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

```
[Test]

public async Task Task_Not_Faulted()

{

    var successfulTask = Task.CompletedTask;



    await Assert.That(successfulTask).IsNotFaulted();

}
```

### IsCompletedSuccessfully / IsNotCompletedSuccessfully (.NET 8+)[​](#iscompletedsuccessfully--isnotcompletedsuccessfully-net-8 "Direct link to IsCompletedSuccessfully / IsNotCompletedSuccessfully (.NET 8+)")

Tests whether a task completed successfully (not faulted or canceled):

```
[Test]

public async Task Task_Completed_Successfully()

{

    var task = Task.CompletedTask;



    await Assert.That(task).IsCompletedSuccessfully();

}
```

```
[Test]

public async Task Task_Not_Completed_Successfully()

{

    var cts = new CancellationTokenSource();

    cts.Cancel();

    var canceledTask = Task.FromCanceled(cts.Token);



    await Assert.That(canceledTask).IsNotCompletedSuccessfully();

}
```

## Timeout Assertions[​](#timeout-assertions "Direct link to Timeout Assertions")

### CompletesWithin[​](#completeswithin "Direct link to CompletesWithin")

Tests that a task completes within a specified time:

```
[Test]

public async Task Task_Completes_Within_Timeout()

{

    var fastTask = Task.Delay(100);



    await Assert.That(fastTask).CompletesWithin(TimeSpan.FromSeconds(1));

}
```

Fails if timeout exceeded:

```
[Test]

public async Task Task_Exceeds_Timeout()

{

    var slowTask = Task.Delay(5000);



    // This will fail - task takes longer than timeout

    // await Assert.That(slowTask).CompletesWithin(TimeSpan.FromMilliseconds(100));

}
```

### WaitsFor[​](#waitsfor "Direct link to WaitsFor")

Polls a value source until a nested assertion passes or the timeout expires. `WaitsFor` takes an assertion-builder lambda (not a bool predicate), so you write the same fluent assertions you would elsewhere:

```
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

        .WaitsFor(src => src.IsEqualTo(true), timeout: TimeSpan.FromSeconds(2));

}
```

`Eventually` is provided as an alias for `WaitsFor` when it reads more naturally.

## Practical Examples[​](#practical-examples "Direct link to Practical Examples")

### API Call Timeout[​](#api-call-timeout "Direct link to API Call Timeout")

```
[Test]

public async Task API_Call_Completes_In_Time()

{

    var apiTask = _httpClient.GetAsync("https://api.example.com/data");



    await Assert.That((Func<Task>)(async () => { await apiTask; })).CompletesWithin(TimeSpan.FromSeconds(5));



    var response = await apiTask;

    await Assert.That(response.IsSuccessStatusCode).IsTrue();

}
```

### Background Task Completion[​](#background-task-completion "Direct link to Background Task Completion")

```
[Test]

public async Task Background_Processing_Completes()

{

    var processingTask = ProcessDataInBackgroundAsync();



    await Assert.That(processingTask).CompletesWithin(TimeSpan.FromMinutes(1));

    await Assert.That(processingTask).IsCompletedSuccessfully();

}
```

### Cancellation Token Handling[​](#cancellation-token-handling "Direct link to Cancellation Token Handling")

```
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

### Async Exception Handling[​](#async-exception-handling "Direct link to Async Exception Handling")

For testing exceptions in async code, use exception assertions:

```
[Test]

public async Task Async_Method_Throws_Exception()

{

    await Assert.That(async () => await FailingOperationAsync())

        .Throws<InvalidOperationException>();

}
```

### Task Result Assertions[​](#task-result-assertions "Direct link to Task Result Assertions")

For `Task<T>`, await the task first, then assert on the result:

```
[Test]

public async Task Task_Returns_Expected_Result()

{

    var task = GetValueAsync();



    // Ensure it completes in time

    await Assert.That((Func<Task>)(async () => { await task; })).CompletesWithin(TimeSpan.FromSeconds(1));



    // Get the result

    var result = await task;



    // Assert on the result

    await Assert.That(result).IsEqualTo(42);

}
```

### Parallel Task Execution[​](#parallel-task-execution "Direct link to Parallel Task Execution")

```
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

### Task State Transitions[​](#task-state-transitions "Direct link to Task State Transitions")

```
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

### Failed Task[​](#failed-task "Direct link to Failed Task")

```
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

### Canceled Task[​](#canceled-task "Direct link to Canceled Task")

```
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

## WhenAll and WhenAny[​](#whenall-and-whenany "Direct link to WhenAll and WhenAny")

### WhenAll Completion[​](#whenall-completion "Direct link to WhenAll Completion")

```
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

### WhenAny Completion[​](#whenany-completion "Direct link to WhenAny Completion")

```
[Test]

public async Task Any_Task_Completes()

{

    var fastTask = Task.Delay(100);

    var slowTask = Task.Delay(5000);



    var firstCompleted = Task.WhenAny(fastTask, slowTask);



    await Assert.That((Func<Task>)(async () => { await firstCompleted; })).CompletesWithin(TimeSpan.FromMilliseconds(500));



    var completed = await firstCompleted;

    await Assert.That(completed).IsSameReferenceAs(fastTask);

}
```

## ValueTask Assertions[​](#valuetask-assertions "Direct link to ValueTask Assertions")

`ValueTask` and `ValueTask<T>` work similarly:

```
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

## Chaining Task Assertions[​](#chaining-task-assertions "Direct link to Chaining Task Assertions")

```
[Test]

public async Task Chained_Task_Assertions()

{

    var task = GetDataAsync();



    await Assert.That((Func<Task>)(async () => { await task; }))

        .CompletesWithin(TimeSpan.FromSeconds(5));



    await Assert.That(task)

        .IsCompleted()

        .And.IsCompletedSuccessfully()

        .And.IsNotCanceled()

        .And.IsNotFaulted();

}
```

## Common Patterns[​](#common-patterns "Direct link to Common Patterns")

### Retry Logic Testing[​](#retry-logic-testing "Direct link to Retry Logic Testing")

```
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



    await Assert.That((Func<Task>)(async () => { await task; })).CompletesWithin(TimeSpan.FromSeconds(10));

    var result = await task;

    await Assert.That(result).IsEqualTo("Success");

}
```

### Debounce Testing[​](#debounce-testing "Direct link to Debounce Testing")

```
[Test]

public async Task Debounced_Operation()

{

    var trigger = new Subject<string>();

    var debouncedTask = trigger

        .Throttle(TimeSpan.FromMilliseconds(500))

        .FirstAsync()

        .ToTask();



    trigger.OnNext("value");



    await Assert.That((Func<Task>)(async () => { await debouncedTask; }))

        .CompletesWithin(TimeSpan.FromSeconds(1));

}
```

### Circuit Breaker Testing[​](#circuit-breaker-testing "Direct link to Circuit Breaker Testing")

```
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

### Producer-Consumer Testing[​](#producer-consumer-testing "Direct link to Producer-Consumer Testing")

```
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

### Rate Limiting[​](#rate-limiting "Direct link to Rate Limiting")

```
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

## Testing Async Disposal[​](#testing-async-disposal "Direct link to Testing Async Disposal")

```
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

## See Also[​](#see-also "Direct link to See Also")

* [Exceptions](/docs/assertions/exceptions.md) - Testing async exceptions
* [DateTime](/docs/assertions/datetime.md) - Timeout and duration testing
* [Boolean](/docs/assertions/boolean.md) - Testing task state booleans
