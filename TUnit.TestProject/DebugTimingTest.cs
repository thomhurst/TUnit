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

        Console.WriteLine($"TestStart at test start: {context.Execution.TestStart?.ToString("O") ?? "NULL"}");
        Console.WriteLine($"TestEnd at test start: {context.Execution.TestEnd?.ToString("O") ?? "NULL"}");

        var timeProvider = TimeProviderContext.Current;
        await timeProvider.Delay(TimeSpan.FromMilliseconds(100));

        Console.WriteLine($"TestStart after delay: {context.Execution.TestStart?.ToString("O") ?? "NULL"}");
        Console.WriteLine($"TestEnd after delay: {context.Execution.TestEnd?.ToString("O") ?? "NULL"}");
    }
}
