using TUnit.Mock;
using TUnit.Mock.Arguments;

namespace TUnit.Mock.Tests;

/// <summary>
/// Test interface with async methods for async mock tests.
/// </summary>
public interface IAsyncService
{
    Task<int> GetValueAsync();
    Task<string> GetNameAsync(string key);
    Task DoWorkAsync();
    ValueTask<int> GetValueValueTaskAsync();
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
        var mock = Mock.Of<IAsyncService>();
        mock.Setup.GetValueAsync().Returns(5);

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
        var mock = Mock.Of<IAsyncService>();
        mock.Setup.GetNameAsync(Arg.Any<string>()).Returns("hello");

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
        var mock = Mock.Of<IAsyncService>();
        mock.Setup.GetNameAsync("key1").Returns("value1");
        mock.Setup.GetNameAsync("key2").Returns("value2");

        IAsyncService service = mock.Object;

        // Act & Assert
        await Assert.That(await service.GetNameAsync("key1")).IsEqualTo("value1");
        await Assert.That(await service.GetNameAsync("key2")).IsEqualTo("value2");
    }

    [Test]
    public async Task Void_Task_Method_Works_In_Loose_Mode()
    {
        // Arrange
        var mock = Mock.Of<IAsyncService>();

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
        var mock = Mock.Of<IAsyncService>();
        mock.Setup.GetValueValueTaskAsync().Returns(42);

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
        var mock = Mock.Of<IAsyncService>();
        mock.Setup.GetValueAsync().Throws<InvalidOperationException>();

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
        var mock = Mock.Of<IAsyncService>();

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
        var mock = Mock.Of<IAsyncService>();

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
        var mock = Mock.Of<IAsyncService>();
        mock.Setup.GetValueAsync().ReturnsSequentially(10, 20, 30);

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
        var mock = Mock.Of<IAsyncService>();
        mock.Setup.GetValueAsync().Returns(5);

        IAsyncService service = mock.Object;

        // Act
        await service.GetValueAsync();
        await service.GetValueAsync();

        // Assert — verify it was called twice
        mock.Verify.GetValueAsync().WasCalled(Times.Exactly(2));
        await Assert.That(true).IsTrue();
    }
}
