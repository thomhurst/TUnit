namespace TUnit.Core.Hooks;

public record AfterTestSessionHookMethod : StaticHookMethod<TestSessionContext>
{
    public override ValueTask ExecuteAsync(TestSessionContext context, CancellationToken cancellationToken)
    {
        return HookExecutor.ExecuteAfterTestSessionHook(MethodInfo, context,
            () => Body!.Invoke(context, cancellationToken)
        );
    }
}