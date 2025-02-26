namespace TUnit.Core.Hooks;

public record AfterTestDiscoveryHookMethod : StaticHookMethod<TestDiscoveryContext>
{
    public override bool Execute(TestDiscoveryContext context, CancellationToken cancellationToken)
    {
        if (Body != null)
        {
            HookExecutor.ExecuteSynchronousAfterTestDiscoveryHook(MethodInfo, context,
                () => Body.Invoke(context, cancellationToken)
            );
            return true;
        }

        return false;
    }

    public override Task ExecuteAsync(TestDiscoveryContext context, CancellationToken cancellationToken)
    {
        return HookExecutor.ExecuteAsynchronousAfterTestDiscoveryHook(MethodInfo, context,
            () => Body!.Invoke(context, cancellationToken)
        );
    }
}