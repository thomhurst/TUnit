namespace TUnit.Core.Hooks;

public record AfterTestDiscoveryHookMethod : StaticHookMethod<TestDiscoveryContext>
{
    public override ValueTask ExecuteAsync(TestDiscoveryContext context, CancellationToken cancellationToken)
    {
        return base.HookExecutor.ExecuteAfterTestDiscoveryHook(MethodInfo, context,
            () => Body!.Invoke(context, cancellationToken)
        );
    }
}
