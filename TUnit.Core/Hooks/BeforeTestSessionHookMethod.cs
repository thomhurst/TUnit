namespace TUnit.Core.Hooks;

public record BeforeTestSessionHookMethod : StaticHookMethod<TestSessionContext>
{
    public override ValueTask ExecuteAsync(TestSessionContext context, CancellationToken cancellationToken)
    {
        return HookExecutor.ExecuteBeforeTestSessionHook(MethodInfo, context,
            () => Body!.Invoke(context, cancellationToken)
        );
    }
}
