using TUnit.Core.Enums;
using TUnit.Core.Interfaces;

namespace TUnit.TestProject;

// Shared execution order tracker
public static class EventReceiverStageTracker
{
    public static readonly List<string> ExecutionOrder = [];
}

// Early stage test start receiver
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class EarlyTestStartReceiverAttribute : Attribute, ITestStartEventReceiver
{
    public int Order => 0;

#if NET
    public EventReceiverStage Stage => EventReceiverStage.Early;
#endif

    public ValueTask OnTestStart(TestContext context)
    {
        EventReceiverStageTracker.ExecutionOrder.Add("EarlyTestStart");
        return default;
    }
}

// Late stage test start receiver (explicit)
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class LateTestStartReceiverAttribute : Attribute, ITestStartEventReceiver
{
    public int Order => 0;

#if NET
    public EventReceiverStage Stage => EventReceiverStage.Late;
#endif

    public ValueTask OnTestStart(TestContext context)
    {
        EventReceiverStageTracker.ExecutionOrder.Add("LateTestStart");
        return default;
    }
}

// Default test start receiver (should behave as Late)
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class DefaultTestStartReceiverAttribute : Attribute, ITestStartEventReceiver
{
    public int Order => 0;

    // No Stage property override - uses default (Late)

    public ValueTask OnTestStart(TestContext context)
    {
        EventReceiverStageTracker.ExecutionOrder.Add("DefaultTestStart");
        return default;
    }
}

// Early stage test end receiver
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class EarlyTestEndReceiverAttribute : Attribute, ITestEndEventReceiver
{
    public int Order => 0;

#if NET
    public EventReceiverStage Stage => EventReceiverStage.Early;
#endif

    public ValueTask OnTestEnd(TestContext context)
    {
        EventReceiverStageTracker.ExecutionOrder.Add("EarlyTestEnd");
        return default;
    }
}

// Late stage test end receiver (explicit)
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class LateTestEndReceiverAttribute : Attribute, ITestEndEventReceiver
{
    public int Order => 0;

#if NET
    public EventReceiverStage Stage => EventReceiverStage.Late;
#endif

    public ValueTask OnTestEnd(TestContext context)
    {
        EventReceiverStageTracker.ExecutionOrder.Add("LateTestEnd");
        return default;
    }
}

// Default test end receiver (should behave as Late)
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class DefaultTestEndReceiverAttribute : Attribute, ITestEndEventReceiver
{
    public int Order => 0;

    // No Stage property override - uses default (Late)

    public ValueTask OnTestEnd(TestContext context)
    {
        EventReceiverStageTracker.ExecutionOrder.Add("DefaultTestEnd");
        return default;
    }
}

// Test receivers with different Order values
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class EarlyTestStartReceiver_Order1Attribute : Attribute, ITestStartEventReceiver
{
    public int Order => 1;

#if NET
    public EventReceiverStage Stage => EventReceiverStage.Early;
#endif

    public ValueTask OnTestStart(TestContext context)
    {
        EventReceiverStageTracker.ExecutionOrder.Add("EarlyTestStart_Order1");
        return default;
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class EarlyTestStartReceiver_Order2Attribute : Attribute, ITestStartEventReceiver
{
    public int Order => 2;

#if NET
    public EventReceiverStage Stage => EventReceiverStage.Early;
#endif

    public ValueTask OnTestStart(TestContext context)
    {
        EventReceiverStageTracker.ExecutionOrder.Add("EarlyTestStart_Order2");
        return default;
    }
}

public class EventReceiverStageTests
{
    [Before(Test)]
    public void SetupTest()
    {
        EventReceiverStageTracker.ExecutionOrder.Clear();
        EventReceiverStageTracker.ExecutionOrder.Add("BeforeTest");
    }

    [After(Test)]
    public void TeardownTest()
    {
        EventReceiverStageTracker.ExecutionOrder.Add("AfterTest");
    }

#if NET
    [Test]
    [EarlyTestStartReceiver]
    public async Task EarlyTestStartReceiver_RunsBeforeBeforeTestHook()
    {
        // Expected order: EarlyTestStart → BeforeTest → Test
        await Assert.That(EventReceiverStageTracker.ExecutionOrder).HasCount().EqualTo(2);
        await Assert.That(EventReceiverStageTracker.ExecutionOrder[0]).IsEqualTo("EarlyTestStart");
        await Assert.That(EventReceiverStageTracker.ExecutionOrder[1]).IsEqualTo("BeforeTest");
    }

    [Test]
    [LateTestStartReceiver]
    public async Task LateTestStartReceiver_RunsAfterBeforeTestHook()
    {
        // Expected order: BeforeTest → LateTestStart → Test
        await Assert.That(EventReceiverStageTracker.ExecutionOrder).HasCount().EqualTo(2);
        await Assert.That(EventReceiverStageTracker.ExecutionOrder[0]).IsEqualTo("BeforeTest");
        await Assert.That(EventReceiverStageTracker.ExecutionOrder[1]).IsEqualTo("LateTestStart");
    }

    [Test]
    [DefaultTestStartReceiver]
    public async Task DefaultTestStartReceiver_RunsAfterBeforeTestHook_AsLateStage()
    {
        // Expected order: BeforeTest → DefaultTestStart → Test
        // Default should behave as Late stage
        await Assert.That(EventReceiverStageTracker.ExecutionOrder).HasCount().EqualTo(2);
        await Assert.That(EventReceiverStageTracker.ExecutionOrder[0]).IsEqualTo("BeforeTest");
        await Assert.That(EventReceiverStageTracker.ExecutionOrder[1]).IsEqualTo("DefaultTestStart");
    }

    [Test]
    [EarlyTestEndReceiver]
    public async Task EarlyTestEndReceiver_RunsBeforeAfterTestHook()
    {
        // Test runs, then in finally block: EarlyTestEnd → AfterTest
        // We verify in AfterEvery hook
        await Task.CompletedTask;
    }

    [After(Test)]
    public async Task VerifyEarlyTestEndReceiverOrder(TestContext context)
    {
        if (context.Metadata.DisplayName.Contains("EarlyTestEndReceiver_RunsBeforeAfterTestHook"))
        {
            // Expected order: BeforeTest → EarlyTestEnd → AfterTest
            await Task.Delay(10); // Small delay to ensure all receivers have executed
            await Assert.That(EventReceiverStageTracker.ExecutionOrder).HasCount().EqualTo(3);
            await Assert.That(EventReceiverStageTracker.ExecutionOrder[0]).IsEqualTo("BeforeTest");
            await Assert.That(EventReceiverStageTracker.ExecutionOrder[1]).IsEqualTo("EarlyTestEnd");
            await Assert.That(EventReceiverStageTracker.ExecutionOrder[2]).IsEqualTo("AfterTest");
        }
    }

    [Test]
    [LateTestEndReceiver]
    public async Task LateTestEndReceiver_RunsAfterAfterTestHook()
    {
        // Test runs, then in finally block: AfterTest → LateTestEnd
        await Task.CompletedTask;
    }

    [After(Test)]
    public async Task VerifyLateTestEndReceiverOrder(TestContext context)
    {
        if (context.Metadata.DisplayName.Contains("LateTestEndReceiver_RunsAfterAfterTestHook"))
        {
            // Expected order: BeforeTest → AfterTest → LateTestEnd
            await Task.Delay(10); // Small delay to ensure all receivers have executed
            await Assert.That(EventReceiverStageTracker.ExecutionOrder).HasCount().EqualTo(3);
            await Assert.That(EventReceiverStageTracker.ExecutionOrder[0]).IsEqualTo("BeforeTest");
            await Assert.That(EventReceiverStageTracker.ExecutionOrder[1]).IsEqualTo("AfterTest");
            await Assert.That(EventReceiverStageTracker.ExecutionOrder[2]).IsEqualTo("LateTestEnd");
        }
    }

    [Test]
    [DefaultTestEndReceiver]
    public async Task DefaultTestEndReceiver_RunsAfterAfterTestHook_AsLateStage()
    {
        // Test runs, then in finally block: AfterTest → DefaultTestEnd
        // Default should behave as Late stage
        await Task.CompletedTask;
    }

    [After(Test)]
    public async Task VerifyDefaultTestEndReceiverOrder(TestContext context)
    {
        if (context.Metadata.DisplayName.Contains("DefaultTestEndReceiver_RunsAfterAfterTestHook_AsLateStage"))
        {
            // Expected order: BeforeTest → AfterTest → DefaultTestEnd
            await Task.Delay(10); // Small delay to ensure all receivers have executed
            await Assert.That(EventReceiverStageTracker.ExecutionOrder).HasCount().EqualTo(3);
            await Assert.That(EventReceiverStageTracker.ExecutionOrder[0]).IsEqualTo("BeforeTest");
            await Assert.That(EventReceiverStageTracker.ExecutionOrder[1]).IsEqualTo("AfterTest");
            await Assert.That(EventReceiverStageTracker.ExecutionOrder[2]).IsEqualTo("DefaultTestEnd");
        }
    }

    [Test]
    [EarlyTestStartReceiver_Order1]
    [EarlyTestStartReceiver_Order2]
    public async Task EarlyStageReceivers_RespectOrderProperty()
    {
        // Expected order: EarlyTestStart_Order1 → EarlyTestStart_Order2 → BeforeTest → Test
        // Order property should still be respected within the same stage
        await Assert.That(EventReceiverStageTracker.ExecutionOrder).HasCount().EqualTo(3);
        await Assert.That(EventReceiverStageTracker.ExecutionOrder[0]).IsEqualTo("EarlyTestStart_Order1");
        await Assert.That(EventReceiverStageTracker.ExecutionOrder[1]).IsEqualTo("EarlyTestStart_Order2");
        await Assert.That(EventReceiverStageTracker.ExecutionOrder[2]).IsEqualTo("BeforeTest");
    }

    [Test]
    [EarlyTestStartReceiver]
    [LateTestStartReceiver]
    [EarlyTestEndReceiver]
    [LateTestEndReceiver]
    public async Task MixedStageReceivers_ExecuteInCorrectOrder()
    {
        // Expected order: EarlyTestStart → BeforeTest → LateTestStart → Test → EarlyTestEnd → AfterTest → LateTestEnd
        await Assert.That(EventReceiverStageTracker.ExecutionOrder).HasCount().EqualTo(3);
        await Assert.That(EventReceiverStageTracker.ExecutionOrder[0]).IsEqualTo("EarlyTestStart");
        await Assert.That(EventReceiverStageTracker.ExecutionOrder[1]).IsEqualTo("BeforeTest");
        await Assert.That(EventReceiverStageTracker.ExecutionOrder[2]).IsEqualTo("LateTestStart");
    }

    [After(Test)]
    public async Task VerifyMixedStageReceiversEndOrder(TestContext context)
    {
        if (context.Metadata.DisplayName.Contains("MixedStageReceivers_ExecuteInCorrectOrder"))
        {
            // Expected final order: EarlyTestStart → BeforeTest → LateTestStart → EarlyTestEnd → AfterTest → LateTestEnd
            await Task.Delay(10); // Small delay to ensure all receivers have executed
            await Assert.That(EventReceiverStageTracker.ExecutionOrder).HasCount().EqualTo(6);
            await Assert.That(EventReceiverStageTracker.ExecutionOrder[0]).IsEqualTo("EarlyTestStart");
            await Assert.That(EventReceiverStageTracker.ExecutionOrder[1]).IsEqualTo("BeforeTest");
            await Assert.That(EventReceiverStageTracker.ExecutionOrder[2]).IsEqualTo("LateTestStart");
            await Assert.That(EventReceiverStageTracker.ExecutionOrder[3]).IsEqualTo("EarlyTestEnd");
            await Assert.That(EventReceiverStageTracker.ExecutionOrder[4]).IsEqualTo("AfterTest");
            await Assert.That(EventReceiverStageTracker.ExecutionOrder[5]).IsEqualTo("LateTestEnd");
        }
    }
#else
    [Test]
    public async Task OnNetStandard_AllReceiversBehaveLikeLateDueToNoDIM()
    {
        // On .NET Standard 2.0, the Stage property doesn't exist due to lack of default interface members
        // All receivers should behave as Late stage (after hooks)
        // This test is just a placeholder to document the behavior
        await Assert.That(true).IsTrue();
    }
#endif
}
