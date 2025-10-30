using System;
using System.Threading.Tasks;
using TUnit.Core;

namespace TUnit.TestProject;

public class DebugTimingTest2
{
    [Test, Repeat(2)]
    public async Task CheckRepeatedTestTiming()
    {
        var context = TestContext.Current!;
        var testId = context.Metadata.TestDetails.TestId;
        
        Console.WriteLine($"[During Test] TestId: {testId}");
        Console.WriteLine($"[During Test] TestStart: {context.Execution.Execution.TestStart?.ToString("O") ?? "NULL"}");
        Console.WriteLine($"[During Test] TestEnd: {context.TestEnd?.ToString("O") ?? "NULL"}");
        Console.WriteLine($"[During Test] Result: {context.Result?.ToString() ?? "NULL"}");
        
        await Task.Delay(300);
    }
    
    [After(Test)]
    public async Task AfterTestHook()
    {
        var context = TestContext.Current!;
        var testId = context.Metadata.TestDetails.TestId;
        
        Console.WriteLine($"[After Hook] TestId: {testId}");
        Console.WriteLine($"[After Hook] TestStart: {context.Execution.Execution.TestStart?.ToString("O") ?? "NULL"}");
        Console.WriteLine($"[After Hook] TestEnd: {context.TestEnd?.ToString("O") ?? "NULL"}");
        Console.WriteLine($"[After Hook] Result.Start: {context.Result?.Start?.ToString("O") ?? "NULL"}");
        Console.WriteLine($"[After Hook] Result.End: {context.Result?.End?.ToString("O") ?? "NULL"}");
        Console.WriteLine("---");
        
        await Task.CompletedTask;
    }
}