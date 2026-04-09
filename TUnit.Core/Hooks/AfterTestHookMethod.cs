namespace TUnit.Core.Hooks;

public record AfterTestHookMethod : StaticHookMethod<TestContext>
{
    public override ValueTask ExecuteAsync(TestContext context, CancellationToken cancellationToken)
    {
        return ResolveEffectiveExecutor(context).ExecuteAfterTestHook(MethodInfo, context,
            () => Body!.Invoke(context, cancellationToken)
        );
    }
}
