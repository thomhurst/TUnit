using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core.Enums;

namespace TUnit.TestProject;

public class ClassHooks
{
    [Before(Class)]
    public static void BeforeHook1()
    {
        // Dummy method
    }
    
    [Before(Class)]
    public static async Task BeforeHook2(ClassHookContext context)
    {
        await Assert.That(context.TestCount).IsEqualTo(1);
    }
    
    [Before(Class), Timeout(30_000)]
    public static void BeforeHook3(CancellationToken cancellationToken)
    {
        // Dummy method
    }
    
    [Before(Class), Timeout(30_000)]
    public static async Task BeforeHook4(ClassHookContext context, CancellationToken cancellationToken)
    {
        await Assert.That(context.TestCount).IsEqualTo(1);
    }
    
    [After(Class)]
    public static void AfterHook1()
    {
        // Dummy method
    }
    
    [After(Class)]
    public static async Task AfterHook2(ClassHookContext context)
    {
        await Assert.That(context.TestCount).IsEqualTo(1);
        await Assert.That(context.Tests.Where(x => x.Result?.Status == Status.Passed)).HasCount().EqualTo(1);
    }
    
    [After(Class), Timeout(30_000)]
    public static void AfterHook3(CancellationToken cancellationToken)
    {
        // Dummy method
    }
    
    [After(Class), Timeout(30_000)]
    public static async Task AfterHook4(ClassHookContext context, CancellationToken cancellationToken)
    {
        await Assert.That(context.TestCount).IsEqualTo(1);
        await Assert.That(context.Tests.Where(x => x.Result?.Status == Status.Passed)).HasCount().EqualTo(1);
    }

    [Test]
    public void Test1()
    {
        // Dummy method
    }
}