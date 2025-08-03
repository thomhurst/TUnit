namespace TUnit.TestProject;

public abstract class AssemblyHooks
{
    private static int _beforeHook1Calls;

    [Before(Assembly)]
    public static void BeforeHook1()
    {
        _beforeHook1Calls++;
    }


#if NET
    [Before(Assembly)]
    public static async Task BeforeHook2(AssemblyHookContext context)
    {
        await Assert.That(context.TestCount).IsPositive();
    }
#endif

#if NET
    [Before(Assembly), Timeout(30_000)]
    public static async Task BeforeHook4(AssemblyHookContext context, CancellationToken cancellationToken)
    {
        await Assert.That(context.TestCount).IsPositive();
    }
#endif

    [After(Assembly)]
    public static async Task AfterHook1()
    {
        await Assert.That(_beforeHook1Calls).IsEqualTo(1);
    }

#if NET
    [After(Assembly)]
    public static async Task AfterHook2(AssemblyHookContext context)
    {
        await Assert.That(context.TestCount).IsPositive();
    }
#endif

#if NET
    [After(Assembly), Timeout(30_000)]
    public static async Task AfterHook4(AssemblyHookContext context, CancellationToken cancellationToken)
    {
        await Assert.That(context.TestCount).IsPositive();
    }
#endif
}
