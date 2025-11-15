using TUnit.Core.Interfaces;

namespace TUnit.TestProject;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class EventReceiverAttribute : Attribute, ITestStartEventReceiver, ITestEndEventReceiver, ITestSkippedEventReceiver
{
    public static readonly List<string> Events =
    [
    ];
    
    public int Order => 0;
    
#if NET
    // Explicitly implement HookStage for testing (should use default Late)
    public TUnit.Core.Enums.HookStage HookStage => TUnit.Core.Enums.HookStage.Late;
#endif
    
    public ValueTask OnTestStart(TestContext context)
    {
        Events.Add($"TestStart: {context. Metadata.DisplayName}");
        return default(ValueTask);
    }
    
    public ValueTask OnTestEnd(TestContext context)
    {
        Events.Add($"TestEnd: {context. Metadata.DisplayName}");
        return default(ValueTask);
    }
    
    public ValueTask OnTestSkipped(TestContext context)
    {
        Events.Add($"TestSkipped: {context. Metadata.DisplayName}");
        return default(ValueTask);
    }
}

public class EventReceiverTests
{
    [Before(Test)]
    public void ClearEvents()
    {
        EventReceiverAttribute.Events.Clear();
    }
    
    [Test]
    [EventReceiver]
    public async Task Test_With_Event_Receiver_Attribute()
    {
        await Task.Delay(10);
        await Assert.That(EventReceiverAttribute.Events).Contains("TestStart: Test_With_Event_Receiver_Attribute");
    }
    
    [Test, Skip("Testing skip event receiver")]
    [EventReceiver]
    public async Task Skipped_Test_With_Event_Receiver()
    {
        await Task.Delay(10);
    }
    
    [After(Test)]
    public async Task VerifyEventsReceived(TestContext context)
    {
        await Task.Delay(50); // Give time for event to be recorded
        
        var displayName = context. Metadata.DisplayName;
        
        // TestEnd receivers run AFTER After(Test) hooks in Late stage (default behavior)
        // So we should only see TestStart at this point, not TestEnd yet
        if (displayName.Contains("Skipped"))
        {
            await Assert.That(EventReceiverAttribute.Events).Contains($"TestSkipped: {displayName}");
        }
        else
        {
            // Just verify TestStart was recorded, TestEnd will be recorded after this hook
            await Assert.That(EventReceiverAttribute.Events).Contains($"TestStart: {displayName}");
        }
    }
}