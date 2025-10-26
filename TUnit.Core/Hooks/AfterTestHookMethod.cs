namespace TUnit.Core.Hooks;

public record AfterTestHookMethod : StaticHookMethod<TestContext>
{
    public override ValueTask ExecuteAsync(TestContext context, CancellationToken cancellationToken)
    {
        // Check if a custom hook executor has been set (e.g., via SetHookExecutor())
        // This ensures static hooks respect the custom executor even in AOT/trimmed builds
        if (context.CustomHookExecutor != null)
        {
            return context.CustomHookExecutor.ExecuteAfterTestHook(MethodInfo, context,
                () => Body!.Invoke(context, cancellationToken)
            );
        }

        // Use the default executor specified at hook registration time
        return HookExecutor.ExecuteAfterTestHook(MethodInfo, context,
            () => Body!.Invoke(context, cancellationToken)
        );
    }
}
