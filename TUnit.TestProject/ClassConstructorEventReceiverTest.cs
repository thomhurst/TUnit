using System.Diagnostics.CodeAnalysis;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace TUnit.TestProject;

public sealed class TestClassConstructorEventReceiver : IClassConstructor, ITestStartEventReceiver, ITestEndEventReceiver
{
    public static readonly List<string> Events = [];

    public async Task<object> Create([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, ClassConstructorMetadata classConstructorMetadata)
    {
        Events.Add("ClassConstructor.Create called");
        return new ClassConstructorEventReceiverTestClass();  
    }

    public ValueTask OnTestEnd(TestContext context)
    {
        Events.Add($"TestEnd: {context.GetDisplayName()}");
        return default;
    }

    public ValueTask OnTestStart(TestContext context)
    {
        Events.Add($"TestStart: {context.GetDisplayName()}");
        return default;
    }
}

[ClassConstructor<TestClassConstructorEventReceiver>]
public sealed class ClassConstructorEventReceiverTestClass
{
    [Before(Test)]
    public void ClearEvents()
    {
        TestClassConstructorEventReceiver.Events.Clear();
    }

    [Test]
    public async Task Test_ClassConstructor_EventReceiver()
    {
        await Task.Delay(10);
        
        // Debug: print what events we have
        Console.WriteLine($"Events collected: [{string.Join(", ", TestClassConstructorEventReceiver.Events)}]");
        
        // First, verify that the ClassConstructor.Create was called
        await Assert.That(TestClassConstructorEventReceiver.Events).Contains("ClassConstructor.Create called");
        
        // This should now pass - the ClassConstructor's event receivers should be called
        await Assert.That(TestClassConstructorEventReceiver.Events).Contains("TestStart: Test_ClassConstructor_EventReceiver()");
    }

    [After(Test)]
    public async Task VerifyEventsReceived(TestContext context)
    {
        await Task.Delay(50); // Give time for event to be recorded
        
        var displayName = context.GetDisplayName();
        
        // Verify that both start and end events were received
        await Assert.That(TestClassConstructorEventReceiver.Events).Contains($"TestEnd: {displayName}");
    }
}