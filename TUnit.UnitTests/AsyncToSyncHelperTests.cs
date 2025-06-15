using System;
using System.Threading.Tasks;
using TUnit.Core;
using TUnit.Core.Helpers;

namespace TUnit.UnitTests;

public class AsyncToSyncHelperTests
{
    [Test]
    public async Task RunSync_WithTask_ReturnsCorrectResult()
    {
        // Arrange
        const string expected = "test result";
        
        // Act
        var result = AsyncToSyncHelper.RunSync(async () =>
        {
            await Task.Delay(10);
            return expected;
        });
        
        // Assert
        await Assert.That(result).IsEqualTo(expected);
    }
    
    [Test]
    public void RunSync_WithException_ThrowsOriginalException()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Test exception");
        
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            AsyncToSyncHelper.RunSync(async () =>
            {
                await Task.Delay(10);
                throw expectedException;
            });
        });
    }
    
    [Test]
    public async Task EnumerateSync_WithAsyncEnumerable_ReturnsAllItems()
    {
        // Arrange
        async IAsyncEnumerable<int> GetNumbersAsync()
        {
            for (int i = 1; i <= 3; i++)
            {
                await Task.Delay(10);
                yield return i;
            }
        }
        
        // Act
        var results = AsyncToSyncHelper.EnumerateSync(GetNumbersAsync()).ToList();
        
        // Assert
        await Assert.That(results).HasCount(3);
        await Assert.That(results[0]).IsEqualTo(1);
        await Assert.That(results[1]).IsEqualTo(2);
        await Assert.That(results[2]).IsEqualTo(3);
    }
    
    [Test]
    public async Task UnwrapTaskResult_WithGenericTask_ReturnsResult()
    {
        // Arrange
        var task = Task.FromResult("test result");
        
        // Act
        var result = AsyncToSyncHelper.UnwrapTaskResult(task);
        
        // Assert
        await Assert.That(result).IsEqualTo("test result");
    }
    
    [Test]
    public async Task UnwrapTaskResult_WithNonGenericTask_ReturnsEmptyArray()
    {
        // Arrange
        var task = Task.CompletedTask;
        
        // Act
        var result = AsyncToSyncHelper.UnwrapTaskResult(task);
        
        // Assert
        await Assert.That(result).IsTypeOf<object[]>();
        await Assert.That(((object[])result).Length).IsEqualTo(0);
    }
    
    [Test]
    public async Task UnwrapTaskResult_WithValueTask_ReturnsResult()
    {
        // Arrange
        var valueTask = new ValueTask<string>(Task.FromResult("test result"));
        
        // Act
        var result = AsyncToSyncHelper.UnwrapTaskResult(valueTask);
        
        // Assert
        await Assert.That(result).IsEqualTo("test result");
    }
}