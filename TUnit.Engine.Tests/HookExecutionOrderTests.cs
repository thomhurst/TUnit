using System.Text;
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
    public async Task VerifyExecutionOrder()
    {
        _executionOrder.Add("Test");
        
        // Verify that BeforeEvery runs before Before
        await Assert.That(_executionOrder).HasCount().EqualTo(3);
        await Assert.That(_executionOrder[0]).IsEqualTo("BeforeEvery");
        await Assert.That(_executionOrder[1]).IsEqualTo("Before");
        await Assert.That(_executionOrder[2]).IsEqualTo("Test");
    }
}