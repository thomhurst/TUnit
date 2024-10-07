using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

public class GlobalTestHooks
{
    [BeforeEvery(Test)]
    public static void SetUp(TestContext testContext)
    {
        testContext.ObjectBag.TryAdd("SetUpCustomTestNameProperty", testContext.TestDetails.TestName);
    }
    
    [AfterEvery(Test)]
    public static void CleanUp(TestContext testContext)
    {
        testContext.ObjectBag.TryAdd("CleanUpCustomTestNameProperty", testContext.TestDetails.TestName);
    }
    
    [BeforeEvery(Class)]
    public static void ClassSetUp(ClassHookContext context)
    {
    }
    
    [AfterEvery(Class)]
    public static void ClassCleanUp(ClassHookContext context)
    {
    }
    
    [BeforeEvery(Assembly)]
    public static void AssemblySetUp(AssemblyHookContext context)
    {
    }
    
    [AfterEvery(Assembly)]
    public static void AssemblyCleanUp(AssemblyHookContext context)
    {
    }
}

public class GlobalTestHooksTests
{
    [Test]
    public async Task SetUpTest1()
    {
        await Assert.That(TestContext.Current?.ObjectBag).HasCount().EqualTo(1);
        await Assert.That(TestContext.Current?.ObjectBag.First().Key).IsEqualTo("SetUpCustomTestNameProperty");
        await Assert.That(TestContext.Current?.ObjectBag.First().Value).IsEquatableOrEqualTo("SetUpTest1");
    }
}