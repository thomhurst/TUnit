namespace TUnit.Core.Hooks;

public record AfterTestHookMethod : StaticHookMethod<TestContext>
{
    public override ValueTask ExecuteAsync(TestContext context, CancellationToken cancellationToken)
    {
        return this.HookExecutor.ExecuteAfterTestHook(MethodInfo, context,
            () => Body!.Invoke(context, cancellationToken)
        );
    }
}
