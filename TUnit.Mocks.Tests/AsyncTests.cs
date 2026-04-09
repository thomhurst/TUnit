using TUnit.Mocks;
using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Tests;

/// <summary>
/// Test interface with async methods for async mock tests.
/// </summary>
public interface IAsyncService
{
    Task<int> GetValueAsync();
    Task<string> GetNameAsync(string key);
    Task DoWorkAsync();
    ValueTask<int> GetValueValueTaskAsync();
    ValueTask<int> ComputeValueTaskAsync(int input);
}

/// <summary>
/// Integration tests for async mock method support.
/// Verifies that async methods (Task{T}, ValueTask{T}, Task) can be
/// configured with unwrapped return types: .Returns(5) instead of .Returns(Task.FromResult(5)).
/// </summary>
public class AsyncTests
{
    [Test]
    public async Task Task_Int_Returns_Unwrapped_Value()
    {
        // Arrange
        var mock = IAsyncService.Mock();
        mock.GetValueAsync().Returns(5);

        IAsyncService service = mock.Object;

        // Act
        var result = await service.GetValueAsync();

        // Assert
        await Assert.That(result).IsEqualTo(5);
    }

    [Test]
    public async Task Task_String_Returns_Unwrapped_Value()
    {
        // Arrange
        var mock = IAsyncService.Mock();
        mock.GetNameAsync(Any()).Returns("hello");

        IAsyncService service = mock.Object;

        // Act
        var result = await service.GetNameAsync("key1");

        // Assert
        await Assert.That(result).IsEqualTo("hello");
    }

    [Test]
    public async Task Task_String_With_Exact_Arg_Match()
    {
        // Arrange
        var mock = IAsyncService.Mock();
        mock.GetNameAsync("key1").Returns("value1");
        mock.GetNameAsync("key2").Returns("value2");

        IAsyncService service = mock.Object;

        // Act & Assert
        await Assert.That(await service.GetNameAsync("key1")).IsEqualTo("value1");
        await Assert.That(await service.GetNameAsync("key2")).IsEqualTo("value2");
    }

    [Test]
    public async Task Void_Task_Method_Works_In_Loose_Mode()
    {
        // Arrange
        var mock = IAsyncService.Mock();

        IAsyncService service = mock.Object;

        // Act — should not throw, should return completed task
        await service.DoWorkAsync();

        // Assert — if we get here, the method worked
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task ValueTask_Int_Returns_Unwrapped_Value()
    {
        // Arrange
        var mock = IAsyncService.Mock();
        mock.GetValueValueTaskAsync().Returns(42);

        IAsyncService service = mock.Object;

        // Act
        var result = await service.GetValueValueTaskAsync();

        // Assert
        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public async Task Async_Method_Throws_Returns_Faulted_Task()
    {
        // Arrange
        var mock = IAsyncService.Mock();
        mock.GetValueAsync().Throws<InvalidOperationException>();

        IAsyncService service = mock.Object;

        // Act — the method should return a faulted task, not throw synchronously
        var task = service.GetValueAsync();

        // Assert — the task is faulted
        await Assert.That(task.IsFaulted).IsTrue();

        try
        {
            await task;
            // Should not reach here
            await Assert.That(false).IsTrue();
        }
        catch (InvalidOperationException)
        {
            // Expected
            await Assert.That(true).IsTrue();
        }
    }

    [Test]
    public async Task Unconfigured_Async_Method_Returns_Default()
    {
        // Arrange
        var mock = IAsyncService.Mock();

        IAsyncService service = mock.Object;

        // Act — unconfigured Task<int> returns Task with default(int)
        var result = await service.GetValueAsync();

        // Assert
        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task Unconfigured_Async_String_Method_Returns_Smart_Default()
    {
        // Arrange
        var mock = IAsyncService.Mock();

        IAsyncService service = mock.Object;

        // Act — unconfigured Task<string> returns Task with "" (smart default)
        var result = await service.GetNameAsync("anything");

        // Assert
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task Async_Method_Sequential_Returns()
    {
        // Arrange
        var mock = IAsyncService.Mock();
        mock.GetValueAsync().ReturnsSequentially(10, 20, 30);

        IAsyncService service = mock.Object;

        // Act & Assert — values returned in order
        await Assert.That(await service.GetValueAsync()).IsEqualTo(10);
        await Assert.That(await service.GetValueAsync()).IsEqualTo(20);
        await Assert.That(await service.GetValueAsync()).IsEqualTo(30);
        // Last value repeats
        await Assert.That(await service.GetValueAsync()).IsEqualTo(30);
    }

    [Test]
    public async Task Async_Method_Verify_Called()
    {
        // Arrange
        var mock = IAsyncService.Mock();
        mock.GetValueAsync().Returns(5);

        IAsyncService service = mock.Object;

        // Act
        await service.GetValueAsync();
        await service.GetValueAsync();

        // Assert — verify it was called twice
        mock.GetValueAsync().WasCalled(Times.Exactly(2));
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task ReturnsAsync_Task_With_TaskCompletionSource()
    {
        // Arrange
        var tcs = new TaskCompletionSource<int>();
        var mock = IAsyncService.Mock();
        mock.GetValueAsync().ReturnsAsync(tcs.Task);

        IAsyncService service = mock.Object;

        // Act — the task is not yet completed
        var task = service.GetValueAsync();
        await Assert.That(task.IsCompleted).IsFalse();

        // Complete the TCS
        tcs.SetResult(42);
        var result = await task;

        // Assert
        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public async Task ReturnsAsync_ValueTask_With_TaskCompletionSource()
    {
        // Arrange
        var tcs = new TaskCompletionSource<int>();
        var mock = IAsyncService.Mock();
        mock.GetValueValueTaskAsync().ReturnsAsync(new ValueTask<int>(tcs.Task));

        IAsyncService service = mock.Object;

        // Act — the ValueTask wraps the TCS
        var vtask = service.GetValueValueTaskAsync();
        await Assert.That(vtask.IsCompleted).IsFalse();

        // Complete the TCS
        tcs.SetResult(99);
        var result = await vtask;

        // Assert
        await Assert.That(result).IsEqualTo(99);
    }

    [Test]
    public async Task ReturnsAsync_Void_Task_With_TaskCompletionSource()
    {
        // Arrange
        var tcs = new TaskCompletionSource();
        var mock = IAsyncService.Mock();
        mock.DoWorkAsync().ReturnsAsync(tcs.Task);

        IAsyncService service = mock.Object;

        // Act — the task is not yet completed
        var task = service.DoWorkAsync();
        await Assert.That(task.IsCompleted).IsFalse();

        // Complete the TCS
        tcs.SetResult();
        await task;

        // Assert — task completed successfully
        await Assert.That(task.Status).IsEqualTo(TaskStatus.RanToCompletion);
    }

    [Test]
    public async Task ReturnsAsync_Factory_Returns_Different_Tasks()
    {
        // Arrange
        var tcs1 = new TaskCompletionSource<int>();
        var tcs2 = new TaskCompletionSource<int>();
        var callCount = 0;
        var mock = IAsyncService.Mock();
        mock.GetValueAsync().ReturnsAsync(() => (++callCount == 1) ? tcs1.Task : tcs2.Task);

        IAsyncService service = mock.Object;

        // Act — first call gets tcs1
        var task1 = service.GetValueAsync();
        tcs1.SetResult(10);
        var result1 = await task1;

        // Second call gets tcs2
        var task2 = service.GetValueAsync();
        tcs2.SetResult(20);
        var result2 = await task2;

        // Assert
        await Assert.That(result1).IsEqualTo(10);
        await Assert.That(result2).IsEqualTo(20);
    }

    [Test]
    public async Task ReturnsAsync_Then_Returns_Sequence()
    {
        // Arrange — mix ReturnsAsync and Returns in a sequence
        var tcs = new TaskCompletionSource<int>();
        var mock = IAsyncService.Mock();
        mock.GetValueAsync()
            .Returns(1)
            .Then()
            .ReturnsAsync(tcs.Task)
            .Then()
            .Returns(3);

        IAsyncService service = mock.Object;

        // First call returns immediately
        var result1 = await service.GetValueAsync();
        await Assert.That(result1).IsEqualTo(1);

        // Second call returns the TCS task (not yet completed)
        var task2 = service.GetValueAsync();
        await Assert.That(task2.IsCompleted).IsFalse();
        tcs.SetResult(2);
        var result2 = await task2;
        await Assert.That(result2).IsEqualTo(2);

        // Third call returns immediately
        var result3 = await service.GetValueAsync();
        await Assert.That(result3).IsEqualTo(3);
    }

    [Test]
    public async Task ReturnsAsync_Already_Completed_Task()
    {
        // Arrange — pass an already-completed task
        var mock = IAsyncService.Mock();
        mock.GetValueAsync().ReturnsAsync(Task.FromResult(123));

        IAsyncService service = mock.Object;

        // Act
        var result = await service.GetValueAsync();

        // Assert
        await Assert.That(result).IsEqualTo(123);
    }

    [Test]
    public async Task ReturnsAsync_Typed_Factory_Receives_Arguments()
    {
        var mock = IAsyncService.Mock();
        mock.GetNameAsync(Any()).ReturnsAsync((string key) => Task.FromResult($"value-{key}"));

        IAsyncService service = mock.Object;

        var result = await service.GetNameAsync("abc");
        await Assert.That(result).IsEqualTo("value-abc");

        var result2 = await service.GetNameAsync("xyz");
        await Assert.That(result2).IsEqualTo("value-xyz");
    }

    [Test]
    public async Task ReturnsAsync_Typed_Factory_ValueTask_Receives_Arguments()
    {
        var mock = IAsyncService.Mock();
        mock.ComputeValueTaskAsync(Any()).ReturnsAsync((int input) => new ValueTask<int>(input * 10));

        IAsyncService service = mock.Object;

        var result = await service.ComputeValueTaskAsync(5);
        await Assert.That(result).IsEqualTo(50);

        var result2 = await service.ComputeValueTaskAsync(3);
        await Assert.That(result2).IsEqualTo(30);
    }

    [Test]
    public async Task ReturnsAsync_Typed_Factory_With_Then_Chain()
    {
        var mock = IAsyncService.Mock();
        mock.GetNameAsync(Any())
            .ReturnsAsync((string key) => Task.FromResult($"first-{key}"))
            .Then()
            .Returns("fallback");

        IAsyncService service = mock.Object;

        var result1 = await service.GetNameAsync("a");
        await Assert.That(result1).IsEqualTo("first-a");

        var result2 = await service.GetNameAsync("b");
        await Assert.That(result2).IsEqualTo("fallback");
    }

    [Test]
    public async Task Typed_Callback_On_Async_Task_Method()
    {
        var mock = IAsyncService.Mock();
        string? capturedKey = null;

        mock.GetNameAsync(Any())
            .Callback((string key) => capturedKey = key)
            .Then()
            .Returns("result");

        IAsyncService service = mock.Object;

        await service.GetNameAsync("myKey");
        await Assert.That(capturedKey).IsEqualTo("myKey");
    }

    [Test]
    public async Task Typed_Callback_On_Async_ValueTask_Method()
    {
        var mock = IAsyncService.Mock();
        int? capturedInput = null;

        mock.ComputeValueTaskAsync(Any())
            .Callback((int input) => capturedInput = input)
            .Then()
            .Returns(0);

        IAsyncService service = mock.Object;

        await service.ComputeValueTaskAsync(42);
        await Assert.That(capturedInput).IsEqualTo(42);
    }

    [Test]
    public async Task Typed_Throws_On_Async_Task_Method()
    {
        var mock = IAsyncService.Mock();
        mock.GetNameAsync(Any())
            .Throws((string key) => new InvalidOperationException($"No value for: {key}"));

        IAsyncService service = mock.Object;

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await service.GetNameAsync("badKey"));
        await Assert.That(ex.Message).Contains("No value for: badKey");
    }

    [Test]
    public async Task Typed_Throws_On_Async_ValueTask_Method()
    {
        var mock = IAsyncService.Mock();
        mock.ComputeValueTaskAsync(Any())
            .Throws((int input) => new ArgumentException($"Invalid input: {input}"));

        IAsyncService service = mock.Object;

        var ex = await Assert.ThrowsAsync<ArgumentException>(async () => await service.ComputeValueTaskAsync(-1));
        await Assert.That(ex.Message).Contains("Invalid input: -1");
    }
}
