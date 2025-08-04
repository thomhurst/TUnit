namespace TUnit.Core.Hooks;

public record AfterTestDiscoveryHookMethod : StaticHookMethod<TestDiscoveryContext>
{
    public override ValueTask ExecuteAsync(TestDiscoveryContext context, CancellationToken cancellationToken)
    {
        return this.HookExecutor.ExecuteAfterTestDiscoveryHook(MethodInfo, context,
            () => Body!.Invoke(context, cancellationToken)
        );
    }
}
