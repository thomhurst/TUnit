namespace TUnit.Core.Hooks;

public record BeforeTestHookMethod : StaticHookMethod<TestContext>
{
    public override ValueTask ExecuteAsync(TestContext context, CancellationToken cancellationToken)
    {
        return HookExecutor.ExecuteBeforeTestHook(MethodInfo, context,
            () => Body!.Invoke(context, cancellationToken)
        );
    }
}