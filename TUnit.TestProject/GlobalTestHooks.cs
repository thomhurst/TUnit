using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

public class GlobalTestHooks
{
    [GlobalBefore(EachTest)]
    public static void SetUp(TestContext testContext)
    {
        testContext.ObjectBag.TryAdd("SetUpCustomTestNameProperty", testContext.TestDetails.TestName);
    }
    
    [GlobalAfter(EachTest)]
    public static void CleanUp(TestContext testContext)
    {
        testContext.ObjectBag.TryAdd("CleanUpCustomTestNameProperty", testContext.TestDetails.TestName);
    }
    
    [GlobalBefore(Class)]
    public static void ClassSetUp(ClassHookContext context)
    {
    }
    
    [GlobalAfter(Class)]
    public static void ClassCleanUp(ClassHookContext context)
    {
    }
    
    [GlobalBefore(Assembly)]
    public static void AssemblySetUp(AssemblyHookContext context)
    {
    }
    
    [GlobalAfter(Assembly)]
    public static void AssemblyCleanUp(AssemblyHookContext context)
    {
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