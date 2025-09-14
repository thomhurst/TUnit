using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using TUnit.Core;

namespace TUnit.TestProject;

public class DebugNotInParallelTest
{
    private static readonly ConcurrentBag<(string testName, DateTime start, DateTime end)> TestRuns = [];

    [After(Test)]
    public async Task RecordTestRun()
    {
        var testName = TestContext.Current!.TestDetails.TestName;
        var start = TestContext.Current.TestStart!.Value.DateTime;
        var end = TestContext.Current.Result!.End!.Value.DateTime;
        
        TestRuns.Add((testName, start, end));
        
        Console.WriteLine($"Test: {testName}");
        Console.WriteLine($"  Start: {start:O}");
        Console.WriteLine($"  End: {end:O}");
        Console.WriteLine($"  Total runs so far: {TestRuns.Count}");
        
        // Check for overlaps
        var thisRun = (testName, start, end);
        var overlaps = TestRuns
            .Where(r => r != thisRun)
            .Where(r => r.start <= end && start <= r.end)
            .ToList();
            
        if (overlaps.Any())
        {
            Console.WriteLine($"  OVERLAPS FOUND: {overlaps.Count}");
            foreach (var overlap in overlaps)
            {
                Console.WriteLine($"    - {overlap.testName}: {overlap.start:O} to {overlap.end:O}");
            }
        }
        else
        {
            Console.WriteLine("  No overlaps");
        }
        
        await Task.CompletedTask;
    }

    [Test, NotInParallel, Repeat(2)]
    public async Task Debug_Test1()
    {
        await Task.Delay(300);
    }

    [Test, NotInParallel, Repeat(2)]
    public async Task Debug_Test2()
    {
        await Task.Delay(300);
    }
}