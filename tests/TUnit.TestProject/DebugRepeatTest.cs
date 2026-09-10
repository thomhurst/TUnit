using System;
using System.Threading;
using System.Threading.Tasks;
using TUnit.Core;

namespace TUnit.TestProject;

public class DebugRepeatTest
{
    private static int _executionCount = 0;
    
    [Test, Repeat(2)]
    public async Task TestWithRepeat()
    {
        var count = Interlocked.Increment(ref _executionCount);
        var context = TestContext.Current!;
        
        Console.WriteLine($"Execution #{count}:");
        Console.WriteLine($"  TestId: {context.Metadata.TestDetails.TestId}");
        Console.WriteLine($"  TestName: {context.Metadata.TestDetails.TestName}");
        Console.WriteLine($"  Thread: {Thread.CurrentThread.ManagedThreadId}");
        
        await Task.Delay(100);
    }
}