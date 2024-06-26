using TUnit.Assertions;
using TUnit.Assertions.Extensions.Numbers;
using TUnit.Core;
using TUnit.Core.Models;

namespace TUnit.TestProject;

public class AssemblyHooks
{
    [AssemblySetUp]
    public static void BeforeHook1()
    {
        // Dummy method
    }
    
    [AssemblySetUp]
    public static async Task BeforeHook2(AssemblyHookContext context)
    {
        await Assert.That(context.TestCount).Is.Positive();
    }
    
    [AssemblyCleanUp]
    public static void AfterHook1()
    {
        // Dummy method
    }
    
    [AssemblyCleanUp]
    public static async Task AfterHook2(AssemblyHookContext context)
    {
        await Assert.That(context.TestCount).Is.Positive();
    }

    [Test]
    public void Test1()
    {
        // Dummy method
    }
}