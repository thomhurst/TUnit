using System;
using System.Threading.Tasks;
using TUnit.Core;
using TUnit.Core.Helpers;

namespace TUnit.UnitTests;

public class AsyncToSyncHelperTests
{
    [Test]
    public void RunSync_WithTask_ReturnsCorrectResult()
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
        Assert.That(result).IsEqualTo(expected);
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
    public void EnumerateSync_WithAsyncEnumerable_ReturnsAllItems()
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
        Assert.That(results).HasCount(3);
        Assert.That(results[0]).IsEqualTo(1);
        Assert.That(results[1]).IsEqualTo(2);
        Assert.That(results[2]).IsEqualTo(3);
    }
    
    [Test]
    public void UnwrapTaskResult_WithGenericTask_ReturnsResult()
    {
        // Arrange
        var task = Task.FromResult("test result");
        
        // Act
        var result = AsyncToSyncHelper.UnwrapTaskResult(task);
        
        // Assert
        Assert.That(result).IsEqualTo("test result");
    }
    
    [Test]
    public void UnwrapTaskResult_WithNonGenericTask_ReturnsEmptyArray()
    {
        // Arrange
        var task = Task.CompletedTask;
        
        // Act
        var result = AsyncToSyncHelper.UnwrapTaskResult(task);
        
        // Assert
        Assert.That(result).IsOfType<object[]>();
        Assert.That(((object[])result).Length).IsEqualTo(0);
    }
    
    [Test]
    public void UnwrapTaskResult_WithValueTask_ReturnsResult()
    {
        // Arrange
        var valueTask = new ValueTask<string>(Task.FromResult("test result"));
        
        // Act
        var result = AsyncToSyncHelper.UnwrapTaskResult(valueTask);
        
        // Assert
        Assert.That(result).IsEqualTo("test result");
    }
}