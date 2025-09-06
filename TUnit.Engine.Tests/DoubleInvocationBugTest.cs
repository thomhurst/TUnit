using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace TUnit.Engine.Tests;

public class DoubleInvocationBugTest : IFirstTestInAssemblyEventReceiver
{
    private static readonly ConcurrentBag<string> _invocations = new();
    private static int _callCount = 0;
    
    public int Order => 0;

    public ValueTask OnFirstTestInAssembly(AssemblyHookContext context, TestContext testContext)
    {
        var count = Interlocked.Increment(ref _callCount);
        var assemblyName = context.Assembly.GetName().FullName ?? "Unknown";
        var eventInfo = $"Call #{count}: Assembly: {assemblyName} at {DateTime.UtcNow:HH:mm:ss.fff}";
        _invocations.Add(eventInfo);
        
        // Log to console for visibility
        Console.WriteLine($"OnFirstTestInAssembly invoked {count} times");
        
        return ValueTask.CompletedTask;
    }
    
    [Test]
    public async Task ShouldOnlyBeCalledOnce()
    {
        await Assert.That(_callCount).IsEqualTo(1);
        Console.WriteLine($"Final count should be 1, actual: {_callCount}");
        foreach (var invocation in _invocations)
        {
            Console.WriteLine($"  {invocation}");
        }
    }
}