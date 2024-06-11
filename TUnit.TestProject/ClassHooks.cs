using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;
using TUnit.Core.Models;

namespace TUnit.TestProject;

public class ClassHooks
{
    [BeforeAllTestsInClass]
    public static void BeforeHook1()
    {
    }
    
    [BeforeAllTestsInClass]
    public static async Task BeforeHook2(ClassHookContext context)
    {
        await Assert.That(context.TestCount).Is.EqualTo(1);
    }
    
    [AfterAllTestsInClass]
    public static void AfterHook1()
    {
    }
    
    [AfterAllTestsInClass]
    public static async Task AfterHook2(ClassHookContext context)
    {
        await Assert.That(context.TestCount).Is.EqualTo(1);
        await Assert.That(context.Tests.Where(x => x.Result?.Status == Status.Passed)).Has.Count().EqualTo(1);
    }

    [Test]
    public void Test1()
    {
    }
}