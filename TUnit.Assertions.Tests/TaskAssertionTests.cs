using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests;

public class TaskAssertionTests
{
    [Test]
    public async Task Test_Task_IsCompleted()
    {
        var task = Task.CompletedTask;
        await Assert.That(task).IsCompleted();
    }

    [Test]
    public async Task Test_Task_IsCompleted_AfterCompletion()
    {
        var task = Task.Run(() => { });
        await task;
        await Assert.That(task).IsCompleted();
    }

    [Test]
    public async Task Test_Task_IsNotCompleted()
    {
        var tcs = new TaskCompletionSource<bool>();
        var task = tcs.Task;
        await Assert.That(task).IsNotCompleted();
    }

    [Test]
    public async Task Test_Task_IsCanceled()
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

    [Test]
    public async Task Test_Task_IsCanceled_WithTaskCompletionSource()
    {
        var tcs = new TaskCompletionSource<bool>();
        tcs.SetCanceled();
        var task = tcs.Task;

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

    [Test]
    public async Task Test_Task_IsNotCanceled()
    {
        var task = Task.CompletedTask;
        await Assert.That(task).IsNotCanceled();
    }

    [Test]
    public async Task Test_Task_IsFaulted()
    {
        var task = Task.Run(() => throw new InvalidOperationException("Test exception"));

        try
        {
            await task;
        }
        catch (InvalidOperationException)
        {
            // Expected
        }

        await Assert.That(task).IsFaulted();
    }

    [Test]
    public async Task Test_Task_IsFaulted_WithTaskCompletionSource()
    {
        var tcs = new TaskCompletionSource<bool>();
        tcs.SetException(new InvalidOperationException("Test"));
        var task = tcs.Task;

        try
        {
            await task;
        }
        catch (InvalidOperationException)
        {
            // Expected
        }

        await Assert.That(task).IsFaulted();
    }

    [Test]
    public async Task Test_Task_IsNotFaulted()
    {
        var task = Task.CompletedTask;
        await Assert.That(task).IsNotFaulted();
    }

#if NET6_0_OR_GREATER
    [Test]
    public async Task Test_Task_IsCompletedSuccessfully()
    {
        var task = Task.CompletedTask;
        await Assert.That(task).IsCompletedSuccessfully();
    }

    [Test]
    public async Task Test_Task_IsCompletedSuccessfully_AfterRun()
    {
        var task = Task.Run(() => 42);
        await task;
        await Assert.That(task).IsCompletedSuccessfully();
    }

    [Test]
    public async Task Test_Task_IsNotCompletedSuccessfully_Faulted()
    {
        var task = Task.Run(() => throw new InvalidOperationException("Test"));

        try
        {
            await task;
        }
        catch (InvalidOperationException)
        {
            // Expected
        }

        await Assert.That(task).IsNotCompletedSuccessfully();
    }

    [Test]
    public async Task Test_Task_IsNotCompletedSuccessfully_Canceled()
    {
        var tcs = new TaskCompletionSource<bool>();
        tcs.SetCanceled();
        var task = tcs.Task;

        try
        {
            await task;
        }
        catch (TaskCanceledException)
        {
            // Expected
        }

        await Assert.That(task).IsNotCompletedSuccessfully();
    }
#endif
}
