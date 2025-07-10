using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Core.Extensions;

namespace TUnit.TestProject;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class EventReceiverAttribute : Attribute, ITestStartEventReceiver, ITestEndEventReceiver, ITestSkippedEventReceiver
{
    public static readonly List<string> Events = new();
    
    public int Order => 0;
    
    public ValueTask OnTestStart(TestContext context)
    {
        Events.Add($"TestStart: {context.GetDisplayName()}");
        return default;
    }
    
    public ValueTask OnTestEnd(TestContext context)
    {
        Events.Add($"TestEnd: {context.GetDisplayName()}");
        return default;
    }
    
    public ValueTask OnTestSkipped(TestContext context)
    {
        Events.Add($"TestSkipped: {context.GetDisplayName()}");
        return default;
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
        await Assert.That(EventReceiverAttribute.Events).Contains("TestStart: Test_With_Event_Receiver_Attribute()");
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
        
        var displayName = context.GetDisplayName();
        
        if (displayName.Contains("Skipped"))
        {
            await Assert.That(EventReceiverAttribute.Events).Contains($"TestSkipped: {displayName}");
        }
        else
        {
            await Assert.That(EventReceiverAttribute.Events).Contains($"TestEnd: {displayName}");
        }
    }
}