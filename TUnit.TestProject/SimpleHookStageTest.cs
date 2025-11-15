using TUnit.Core.Enums;
using TUnit.Core.Interfaces;

namespace TUnit.TestProject;

public class SimpleHookStageTest
{
    [Test]
    [SimpleEarlyReceiver]
    public async Task SimpleTest()
    {
        Console.WriteLine("Test executed");
        await Task.CompletedTask;
    }
}

#if NET
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class SimpleEarlyReceiverAttribute : Attribute, ITestStartEventReceiver
{
    public int Order => 0;
    public HookStage HookStage => HookStage.Early;
    
    public ValueTask OnTestStart(TestContext context)
    {
        Console.WriteLine("SimpleEarlyReceiver.OnTestStart called!");
        return default;
    }
}
#endif
