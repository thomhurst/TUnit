using System;
using System.Threading.Tasks;
using TUnit.Core;

namespace TUnit.TestProject;

public class DebugTimingTest
{
    [Test]
    public async Task CheckTimingProperties()
    {
        var context = TestContext.Current!;
        
        Console.WriteLine($"TestStart at test start: {context.Execution.Execution.TestStart?.ToString("O") ?? "NULL"}");
        Console.WriteLine($"TestEnd at test start: {context.TestEnd?.ToString("O") ?? "NULL"}");
        
        await Task.Delay(100);
        
        Console.WriteLine($"TestStart after delay: {context.Execution.Execution.TestStart?.ToString("O") ?? "NULL"}");
        Console.WriteLine($"TestEnd after delay: {context.TestEnd?.ToString("O") ?? "NULL"}");
    }
}