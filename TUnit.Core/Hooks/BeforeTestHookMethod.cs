namespace TUnit.Core.Hooks;

public record BeforeTestHookMethod : StaticHookMethod<TestContext>
{
    public override ValueTask ExecuteAsync(TestContext context, CancellationToken cancellationToken)
    {
        // Precedence: the hook's own HookExecutor wins if it was set explicitly (e.g. via
        // [HookExecutor<T>] on the hook method). Only fall back to the test-level
        // CustomHookExecutor when the hook is still on the DefaultExecutor — this preserves
        // the #2666 scenario where SetHookExecutor on the test class fills in for hooks
        // that didn't specify their own executor, without overriding hooks that did.
        var executor = HookExecutor;
        if (ReferenceEquals(executor, DefaultExecutor.Instance) && context.CustomHookExecutor != null)
        {
            executor = context.CustomHookExecutor;
        }

        return executor.ExecuteBeforeTestHook(MethodInfo, context,
            () => Body!.Invoke(context, cancellationToken)
        );
    }
}
