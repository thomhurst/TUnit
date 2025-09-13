using System;
using System.Threading.Tasks;
using TUnit.Assertions;

namespace TUnit.UnitTests;

// Test implementation of Context for thread safety testing
internal class TestableContext : Context
{
    public TestableContext() : base(null) { }

    internal override void SetAsyncLocalContext() { }
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

        for (var i = 0; i < 20; i++)
        {
            var taskId = i;
            tasks.Add(Task.Run(async () =>
            {
                for (var j = 0; j < 50; j++)
                {
                    // Write to output
                    context.OutputWriter.WriteLine($"Task {taskId}, iteration {j}");

                    // Write to error output
                    context.ErrorOutputWriter.WriteLine($"Error Task {taskId}, iteration {j}");

                    // Add randomness to increase chance of race condition
                    await Task.Delay(random.Next(1, 3));

                    // Read output multiple times - this would cause ArgumentOutOfRangeException before the fix
                    // We're testing thread safety of reads, not checking specific content
                    for (var read = 0; read < 5; read++)
                    {
                        var output = context.GetStandardOutput();
                        var errorOutput = context.GetErrorOutput();

                        // Just verify we can read without exceptions
                        // No assertions on content since other tasks may or may not have written yet
                        await Task.Yield();
                    }
                }
            }));
        }

        // This should complete without any ArgumentOutOfRangeException
        await Task.WhenAll(tasks);

        // Verify final output contains data from multiple tasks
        var finalOutput = context.GetStandardOutput();
        var finalErrorOutput = context.GetErrorOutput();

        // Should have output from multiple tasks
        await Assert.That(finalOutput).IsNotEmpty();
        await Assert.That(finalErrorOutput).IsNotEmpty();

        // Verify we have output from at least several different tasks (not all 20 due to potential timing)
        var foundTasks = 0;
        for (var i = 0; i < 20; i++)
        {
            if (finalOutput.Contains($"Task {i},"))
            {
                foundTasks++;
            }
        }
        await Assert.That(foundTasks).IsGreaterThan(5); // At least 5 tasks should have written something
    }
}
