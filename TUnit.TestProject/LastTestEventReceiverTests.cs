using TUnit.Core;
using TUnit.Core.Interfaces;

namespace TUnit.TestProject;

public class LastTestEventReceiverTests
{
    public static readonly List<string> Events = [];

    [Before(Test)]
    public void ClearEvents()
    {
        Events.Clear();
    }

    [Test]
    [LastTestEventReceiver]
    public async Task Test1()
    {
        await Task.Delay(10);
    }

    [Test]
    [LastTestEventReceiver]
    public async Task Test2()
    {
        await Task.Delay(10);
    }

    [Test]
    [LastTestEventReceiver]
    public async Task Test3()
    {
        await Task.Delay(10);
    }

    [After(Test)]
    public async Task VerifyLastTestEventFired(TestContext context)
    {
        // Give some time for async event receivers to complete
        await Task.Delay(100);

        var displayName = context.GetDisplayName();

        // After the last test (Test3), we should have the last test event recorded
        if (displayName.Contains("Test3"))
        {
            await Assert.That(Events).Contains("LastTestInClass");
        }
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class LastTestEventReceiverAttribute : Attribute,
    ITestStartEventReceiver,
    ITestEndEventReceiver,
    ILastTestInClassEventReceiver
{
    public int Order => 0;

    public ValueTask OnTestStart(TestContext context)
    {
        LastTestEventReceiverTests.Events.Add($"TestStart: {context.GetDisplayName()}");
        return default;
    }

    public ValueTask OnTestEnd(TestContext context)
    {
        LastTestEventReceiverTests.Events.Add($"TestEnd: {context.GetDisplayName()}");
        return default;
    }

    public ValueTask OnLastTestInClass(ClassHookContext context, TestContext testContext)
    {
        LastTestEventReceiverTests.Events.Add("LastTestInClass");
        return default;
    }
}

// Separate test class to test assembly-level last test event
public class LastTestInAssemblyEventReceiverTests
{
    public static readonly List<string> Events = [];

    [Before(Test)]
    public void ClearEvents()
    {
        Events.Clear();
    }

    [Test]
    [LastTestInAssemblyEventReceiver]
    public async Task AssemblyTest()
    {
        await Task.Delay(10);
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class LastTestInAssemblyEventReceiverAttribute : Attribute,
    ILastTestInAssemblyEventReceiver
{
    public int Order => 0;

    public ValueTask OnLastTestInAssembly(AssemblyHookContext context, TestContext testContext)
    {
        LastTestInAssemblyEventReceiverTests.Events.Add("LastTestInAssembly");
        return default;
    }
}

// Test for skipped event receivers
public class SkippedEventReceiverTests
{
    public static readonly List<string> Events = [];
    public static string? CapturedSkipReason = null;

    [Before(Test)]
    public void ClearEvents()
    {
        Events.Clear();
        CapturedSkipReason = null;
    }

    [Test, Skip("Testing skip event with custom reason")]
    [SkipEventReceiverAttribute]
    public async Task SkippedTestWithCustomReason()
    {
        await Task.Delay(10);
    }

    [After(Test)]
    public async Task VerifySkipEventFired(TestContext context)
    {
        // Give some time for async event receivers to complete
        await Task.Delay(100);

        if (context.GetDisplayName().Contains("SkippedTestWithCustomReason"))
        {
            await Assert.That(Events).Contains("TestSkipped");
            await Assert.That(Events).Contains("TestEnd");
            await Assert.That(CapturedSkipReason).IsEqualTo("Testing skip event with custom reason");
        }
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class SkipEventReceiverAttribute : Attribute,
    ITestSkippedEventReceiver,
    ITestEndEventReceiver
{
    public int Order => 0;

    public ValueTask OnTestSkipped(TestContext context)
    {
        SkippedEventReceiverTests.Events.Add("TestSkipped");
        SkippedEventReceiverTests.CapturedSkipReason = context.SkipReason;
        return default;
    }

    public ValueTask OnTestEnd(TestContext context)
    {
        SkippedEventReceiverTests.Events.Add("TestEnd");
        return default;
    }
}

// Test for runtime skip using Skip.Test()
public class RuntimeSkipEventReceiverTests
{
    public static readonly List<string> Events = [];
    public static string? CapturedSkipReason = null;

    [Before(Test)]
    public void ClearEvents()
    {
        Events.Clear();
        CapturedSkipReason = null;
    }

    [Test]
    [RuntimeSkipEventReceiverAttribute]
    public void RuntimeSkippedTestWithCustomReason()
    {
        Skip.Test("Custom runtime skip reason");
    }

    [After(Test)]
    public async Task VerifyRuntimeSkipEventFired(TestContext context)
    {
        // Give some time for async event receivers to complete
        await Task.Delay(100);

        if (context.GetDisplayName().Contains("RuntimeSkippedTestWithCustomReason"))
        {
            // Verify skip reason is preserved
            await Assert.That(CapturedSkipReason).IsEqualTo("Custom runtime skip reason");

            // Verify both skipped and end events are fired
            await Assert.That(Events).Contains("TestSkipped");
            await Assert.That(Events).Contains("TestEnd");

            // Verify TestEnd is called exactly once (not twice)
            var testEndCount = Events.Count(e => e == "TestEnd");
            await Assert.That(testEndCount).IsEqualTo(1);
        }
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RuntimeSkipEventReceiverAttribute : Attribute,
    ITestSkippedEventReceiver,
    ITestEndEventReceiver
{
    public int Order => 0;

    public ValueTask OnTestSkipped(TestContext context)
    {
        RuntimeSkipEventReceiverTests.Events.Add("TestSkipped");
        RuntimeSkipEventReceiverTests.CapturedSkipReason = context.SkipReason;
        return default;
    }

    public ValueTask OnTestEnd(TestContext context)
    {
        RuntimeSkipEventReceiverTests.Events.Add("TestEnd");
        return default;
    }
}
