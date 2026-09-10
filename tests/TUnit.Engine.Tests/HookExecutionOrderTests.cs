using Shouldly;
using TUnit.Core;

namespace TUnit.Engine.Tests;

public sealed class GlobalHookExecutionOrderSetup
{
    [BeforeEvery(Test)]
    public static void GlobalSetup(TestContext context)
    {
        if (context.Metadata.TestDetails.ClassType != typeof(HookExecutionOrderTests))
        {
            return;
        }

        var order = context.StateBag.GetOrAdd<List<string>>("executionOrder", _ => []);
        order.Add("BeforeEvery");
    }
}

public class HookExecutionOrderTests
{
    [Before(Test)]
    public void InstanceSetup()
    {
        var order = TestContext.Current!.StateBag.GetOrAdd<List<string>>("executionOrder", _ => []);
        order.Add("Before");
    }

    [Test]
    public void VerifyExecutionOrder()
    {
        var order = TestContext.Current!.StateBag.GetOrAdd<List<string>>("executionOrder", _ => []);
        order.Add("Test");

        // Verify that BeforeEvery runs before Before
        order.Count.ShouldBe(3);
        order[0].ShouldBe("BeforeEvery");
        order[1].ShouldBe("Before");
        order[2].ShouldBe("Test");
    }
}
