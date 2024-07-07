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
    
    [AssemblySetUp, Timeout(30_000)]
    public static void BeforeHook3(CancellationToken cancellationToken)
    {
        // Dummy method
    }
    
    [AssemblySetUp, Timeout(30_000)]
    public static async Task BeforeHook4(AssemblyHookContext context, CancellationToken cancellationToken)
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
    
    [AssemblyCleanUp, Timeout(30_000)]
    public static void AfterHook3(CancellationToken cancellationToken)
    {
        // Dummy method
    }
    
    [AssemblyCleanUp, Timeout(30_000)]
    public static async Task AfterHook4(AssemblyHookContext context, CancellationToken cancellationToken)
    {
        await Assert.That(context.TestCount).Is.Positive();
    }

    [Test]
    public void Test1()
    {
        // Dummy method
    }
}