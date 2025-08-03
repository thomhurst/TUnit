using System;
using System.Threading.Tasks;
using TUnit.Assertions;

namespace TUnit.UnitTests;

// Test implementation of Context for thread safety testing
internal class TestableContext : Context
{
    public TestableContext() : base(null) { }
    
    internal override void RestoreContextAsyncLocal() { }
}

public class ContextThreadSafetyTests
{
    [Test]
    public async Task GetStandardOutput_WithConcurrentAccess_ShouldNotThrowException()
    {
        var context = new TestableContext();

        // Simulate concurrent writes and reads that would cause the original issue
        var tasks = new List<Task>();
        var random = new Random();

        for (int i = 0; i < 20; i++)
        {
            var taskId = i;
            tasks.Add(Task.Run(async () =>
            {
                for (int j = 0; j < 50; j++)
                {
                    // Write to output
                    context.OutputWriter.WriteLine($"Task {taskId}, iteration {j}");
                    
                    // Add randomness to increase chance of race condition
                    await Task.Delay(random.Next(1, 3));
                    
                    // Read output - this would cause ArgumentOutOfRangeException before the fix
                    var output = context.GetStandardOutput();
                    
                    // Verify output contains expected content
                    await Assert.That(output).Contains($"Task {taskId}");
                    
                    // Write to error output  
                    context.ErrorOutputWriter.WriteLine($"Error Task {taskId}, iteration {j}");
                    
                    // Read error output
                    var errorOutput = context.GetErrorOutput();
                    
                    // Verify error output contains expected content
                    await Assert.That(errorOutput).Contains($"Error Task {taskId}");
                }
            }));
        }

        // This should complete without any ArgumentOutOfRangeException
        await Task.WhenAll(tasks);
        
        // Verify final output contains data from all tasks
        var finalOutput = context.GetStandardOutput();
        var finalErrorOutput = context.GetErrorOutput();
        
        await Assert.That(finalOutput).IsNotEmpty();
        await Assert.That(finalErrorOutput).IsNotEmpty();
    }
}