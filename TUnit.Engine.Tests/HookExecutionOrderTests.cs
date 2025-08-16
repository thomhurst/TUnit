using Shouldly;
using TUnit.Core;

namespace TUnit.Engine.Tests;

public sealed class GlobalHookExecutionOrderSetup
{
    [BeforeEvery(Test)]
    public static void GlobalSetup(TestContext context)
    {
        HookExecutionOrderTests._executionOrder.Clear(); // Clear before each test
        HookExecutionOrderTests._executionOrder.Add("BeforeEvery");
    }
}

public class HookExecutionOrderTests
{
    internal static readonly List<string> _executionOrder = [];

    [Before(Test)]
    public void InstanceSetup()
    {
        _executionOrder.Add("Before");
    }

    [Test]
    public void VerifyExecutionOrder()
    {
        _executionOrder.Add("Test");
        
        // Verify that BeforeEvery runs before Before
        _executionOrder.Count.ShouldBe(3);
        _executionOrder[0].ShouldBe("BeforeEvery");
        _executionOrder[1].ShouldBe("Before");
        _executionOrder[2].ShouldBe("Test");
    }
}