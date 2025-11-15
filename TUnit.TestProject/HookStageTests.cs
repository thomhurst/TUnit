using TUnit.Core.Enums;
using TUnit.Core.Interfaces;

namespace TUnit.TestProject;

/// <summary>
/// Tests to verify HookStage functionality for ITestStartEventReceiver and ITestEndEventReceiver.
/// These tests verify that Early and Late stage event receivers execute in the correct order
/// relative to instance-level hooks.
/// </summary>
public class HookStageTests
{
    public static readonly List<string> ExecutionOrder = [];

    [Before(Test)]
    public void Setup()
    {
        // Don't clear - we want to track everything including early receivers
        // Only clear at the very first entry (which would be the early receiver)
        if (ExecutionOrder.Count == 0 || ExecutionOrder.Last() == "Test Method")
        {
            ExecutionOrder.Clear();
        }
        ExecutionOrder.Add("Before(Test) Hook");
    }

    [Test]
    [EarlyTestStartReceiver]
    [LateTestStartReceiver]
    public async Task TestStart_Receivers_Execute_In_Correct_Order()
    {
        ExecutionOrder.Add("Test Method");
        
        // Early receiver should have run before [Before(Test)] hook
        // Late receiver should have run after [Before(Test)] hook but before test
        
#if NET
        // On NET 8.0+, HookStage is supported
        // Expected order: EarlyTestStartReceiver → Before(Test) → LateTestStartReceiver → Test Method
        // Note: There might be other framework hooks, so we check for relative ordering
        var earlyIndex = ExecutionOrder.IndexOf("EarlyTestStartReceiver.OnTestStart");
        var beforeIndex = ExecutionOrder.IndexOf("Before(Test) Hook");
        var lateIndex = ExecutionOrder.IndexOf("LateTestStartReceiver.OnTestStart");
        var testIndex = ExecutionOrder.IndexOf("Test Method");
        
        await Assert.That(earlyIndex).IsGreaterThanOrEqualTo(0);
        await Assert.That(beforeIndex).IsGreaterThan(earlyIndex);
        await Assert.That(lateIndex).IsGreaterThan(beforeIndex);
        await Assert.That(testIndex).IsGreaterThan(lateIndex);
#else
        // On older frameworks, all receivers run at Late stage (current behavior)
        await Assert.That(ExecutionOrder)
            .HasCount()
            .GreaterThanOrEqualTo(2);
#endif
    }

    [Test]
    [EarlyTestEndReceiver]
    [LateTestEndReceiver]
    public async Task TestEnd_Receivers_Execute_In_Correct_Order()
    {
        ExecutionOrder.Add("Test Method");
        
        // Verification happens in After(Test) hook
        await Task.CompletedTask;
    }

    [After(Test)]
    public async Task VerifyTestEndOrder(TestContext context)
    {
        ExecutionOrder.Add("After(Test) Hook");
        
        if (context.Metadata.DisplayName.Contains(nameof(TestEnd_Receivers_Execute_In_Correct_Order)))
        {
#if NET
            // On NET 8.0+:
            // Expected order: Before(Test) → Test Method → EarlyTestEndReceiver → After(Test) → LateTestEndReceiver
            // We can verify up to After(Test) here; LateTestEndReceiver runs after this method
            var beforeIndex = ExecutionOrder.IndexOf("Before(Test) Hook");
            var testIndex = ExecutionOrder.IndexOf("Test Method");
            var earlyIndex = ExecutionOrder.IndexOf("EarlyTestEndReceiver.OnTestEnd");
            var afterIndex = ExecutionOrder.IndexOf("After(Test) Hook");
            
            await Assert.That(beforeIndex).IsGreaterThanOrEqualTo(0);
            await Assert.That(testIndex).IsGreaterThan(beforeIndex);
            await Assert.That(earlyIndex).IsGreaterThan(testIndex);
            await Assert.That(afterIndex).IsGreaterThan(earlyIndex);
#else
            // On older frameworks, all receivers run after After(Test) hook
            await Assert.That(ExecutionOrder)
                .HasCount()
                .GreaterThanOrEqualTo(3);
#endif
        }
    }
}

#if NET
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class EarlyTestStartReceiverAttribute : Attribute, ITestStartEventReceiver
{
    public int Order => 0;
    
    public HookStage HookStage => HookStage.Early;
    
    public ValueTask OnTestStart(TestContext context)
    {
        HookStageTests.ExecutionOrder.Add("EarlyTestStartReceiver.OnTestStart");
        return default;
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class LateTestStartReceiverAttribute : Attribute, ITestStartEventReceiver
{
    public int Order => 0;
    
    public HookStage HookStage => HookStage.Late;
    
    public ValueTask OnTestStart(TestContext context)
    {
        HookStageTests.ExecutionOrder.Add("LateTestStartReceiver.OnTestStart");
        return default;
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class EarlyTestEndReceiverAttribute : Attribute, ITestEndEventReceiver
{
    public int Order => 0;
    
    public HookStage HookStage => HookStage.Early;
    
    public ValueTask OnTestEnd(TestContext context)
    {
        HookStageTests.ExecutionOrder.Add("EarlyTestEndReceiver.OnTestEnd");
        return default;
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class LateTestEndReceiverAttribute : Attribute, ITestEndEventReceiver
{
    public int Order => 0;
    
    public HookStage HookStage => HookStage.Late;
    
    public ValueTask OnTestEnd(TestContext context)
    {
        HookStageTests.ExecutionOrder.Add("LateTestEndReceiver.OnTestEnd");
        return default;
    }
}
#else
// On older frameworks without default interface implementation support,
// we create stub attributes that don't implement the event receiver interfaces
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class EarlyTestStartReceiverAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class LateTestStartReceiverAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class EarlyTestEndReceiverAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class LateTestEndReceiverAttribute : Attribute
{
}
#endif
