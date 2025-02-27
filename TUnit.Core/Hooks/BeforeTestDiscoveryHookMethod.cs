namespace TUnit.Core.Hooks;

public record BeforeTestDiscoveryHookMethod : StaticHookMethod<BeforeTestDiscoveryContext>
{
    public override bool Execute(BeforeTestDiscoveryContext context, CancellationToken cancellationToken)
    {
        if (Body != null)
        {
            HookExecutor.ExecuteSynchronousBeforeTestDiscoveryHook(MethodInfo, context,
                () => Body.Invoke(context, cancellationToken)
            );
            return true;
        }

        return false;
    }

    public override Task ExecuteAsync(BeforeTestDiscoveryContext context, CancellationToken cancellationToken)
    {
        return HookExecutor.ExecuteAsynchronousBeforeTestDiscoveryHook(MethodInfo, context,
            () => Body!.Invoke(context, cancellationToken)
        );
    }
}