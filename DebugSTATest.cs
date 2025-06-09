using System.Threading;
using TUnit.Core.Executors;

namespace TUnit.TestProject;

public class DebugSTATest
{
    [Test, STAThreadExecutor]
    public async Task DebugSTAExecution()
    {
        // Let's trace what happens step by step
        Console.WriteLine($"Initial apartment state: {Thread.CurrentThread.GetApartmentState()}");
        Console.WriteLine($"Thread ID: {Thread.CurrentThread.ManagedThreadId}");
        Console.WriteLine($"Is background: {Thread.CurrentThread.IsBackground}");
        
        // Try a small async operation
        await Task.Delay(1);
        
        Console.WriteLine($"After Task.Delay apartment state: {Thread.CurrentThread.GetApartmentState()}");
        Console.WriteLine($"After Task.Delay Thread ID: {Thread.CurrentThread.ManagedThreadId}");
        
        // Try ConfigureAwait(false)
        await Task.Delay(1).ConfigureAwait(false);
        
        Console.WriteLine($"After ConfigureAwait(false) apartment state: {Thread.CurrentThread.GetApartmentState()}");
        Console.WriteLine($"After ConfigureAwait(false) Thread ID: {Thread.CurrentThread.ManagedThreadId}");
    }
}
