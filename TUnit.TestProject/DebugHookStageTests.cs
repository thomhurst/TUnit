using TUnit.Core.Enums;
using TUnit.Core.Interfaces;

namespace TUnit.TestProject;

public class DebugHookStageTests
{
    public static readonly List<string> ExecutionOrder = [];

    [Before(Test)]
    public void Setup()
    {
        ExecutionOrder.Clear();
        ExecutionOrder.Add("Before(Test) Hook");
        Console.WriteLine($"[Before(Test)] Current order: {string.Join(", ", ExecutionOrder)}");
    }

    [Test]
    [DebugEarlyTestStartReceiver]
    [DebugLateTestStartReceiver]
    public async Task Debug_TestStart_Order()
    {
        ExecutionOrder.Add("Test Method");
        Console.WriteLine($"[Test Method] Current order: {string.Join(", ", ExecutionOrder)}");
        
        // Print actual order for debugging
        foreach (var item in ExecutionOrder.Select((v, i) => new { v, i }))
        {
            Console.WriteLine($"  [{item.i}] = {item.v}");
        }
        
        await Task.CompletedTask;
    }
}

#if NET
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class DebugEarlyTestStartReceiverAttribute : Attribute, ITestStartEventReceiver
{
    public int Order => 0;
    
    public HookStage HookStage => HookStage.Early;
    
    public ValueTask OnTestStart(TestContext context)
    {
        DebugHookStageTests.ExecutionOrder.Add("EarlyTestStartReceiver.OnTestStart");
        Console.WriteLine($"[Early Receiver] Current order: {string.Join(", ", DebugHookStageTests.ExecutionOrder)}");
        return default;
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class DebugLateTestStartReceiverAttribute : Attribute, ITestStartEventReceiver
{
    public int Order => 0;
    
    public HookStage HookStage => HookStage.Late;
    
    public ValueTask OnTestStart(TestContext context)
    {
        DebugHookStageTests.ExecutionOrder.Add("LateTestStartReceiver.OnTestStart");
        Console.WriteLine($"[Late Receiver] Current order: {string.Join(", ", DebugHookStageTests.ExecutionOrder)}");
        return default;
    }
}
#endif
