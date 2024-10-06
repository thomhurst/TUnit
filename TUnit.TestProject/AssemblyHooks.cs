using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

public abstract class AssemblyHooks
{
    private static int _beforeHook1Calls;
    
    [Before(Assembly)]
    public static void BeforeHook1()
    {
        _beforeHook1Calls++;
    }
    
    [Before(Assembly)]
    public static async Task BeforeHook2(AssemblyHookContext context)
    {
        await Assert.That(context.TestCount).IsPositive();
    }
    
    [Before(Assembly), Timeout(30_000)]
    public static void BeforeHook3(CancellationToken cancellationToken)
    {
        // Dummy method
    }
    
    [Before(Assembly), Timeout(30_000)]
    public static async Task BeforeHook4(AssemblyHookContext context, CancellationToken cancellationToken)
    {
        await Assert.That(context.TestCount).IsPositive();
    }
    
    [After(Assembly)]
    public static async Task AfterHook1()
    {
        await Assert.That(_beforeHook1Calls).IsEqualTo(1);
    }
    
    [After(Assembly)]
    public static async Task AfterHook2(AssemblyHookContext context)
    {
        await Assert.That(context.TestCount).IsPositive();
    }
    
    [After(Assembly), Timeout(30_000)]
    public static void AfterHook3(CancellationToken cancellationToken)
    {
        // Dummy method
    }
    
    [After(Assembly), Timeout(30_000)]
    public static async Task AfterHook4(AssemblyHookContext context, CancellationToken cancellationToken)
    {
        await Assert.That(context.TestCount).IsPositive();
    }
}