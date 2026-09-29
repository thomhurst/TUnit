namespace TUnit.Core.Hooks;

public record AfterTestDiscoveryHookMethod : StaticHookMethod<TestDiscoveryContext>
{
    public override ValueTask ExecuteAsync(TestDiscoveryContext context, CancellationToken cancellationToken)
    {
        return HookExecutor.ExecuteAfterTestDiscoveryHook(MethodInfo, context,
            () => HookBodyInvoker.InvokeAsync(Body!, context, cancellationToken)
        );
    }
}
