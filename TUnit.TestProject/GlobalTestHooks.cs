using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.TestProject;

public class GlobalTestHooks
{
    [Before(EachTestGlobally)]
    public static void SetUp(TestContext testContext)
    {
        testContext.ObjectBag.TryAdd("SetUpCustomTestNameProperty", testContext.TestDetails.TestName);
    }
    
    [After(EachTestGlobally)]
    public static void CleanUp(TestContext testContext)
    {
        testContext.ObjectBag.TryAdd("CleanUpCustomTestNameProperty", testContext.TestDetails.TestName);
    }
}

public class GlobalTestHooksTests
{
    [Test]
    public async Task SetUpTest1()
    {
        await Assert.That(TestContext.Current?.ObjectBag).Has.Count().EqualTo(1);
        await Assert.That(TestContext.Current?.ObjectBag.First().Key).Is.EqualTo("SetUpCustomTestNameProperty");
        await Assert.That(TestContext.Current?.ObjectBag.First().Value).Is.EqualTo("SetUpTest1");
    }
}