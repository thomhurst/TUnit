namespace TUnit.Core.Hooks;

public record AfterTestSessionHookMethod : StaticHookMethod<TestSessionContext>
{
    public override bool Execute(TestSessionContext context, CancellationToken cancellationToken)
    {
        if (Body != null)
        {
            HookExecutor.ExecuteSynchronousAfterTestSessionHook(MethodInfo, context,
                () => Body.Invoke(context, cancellationToken)
            );
            return true;
        }

        return false;
    }

    public override Task ExecuteAsync(TestSessionContext context, CancellationToken cancellationToken)
    {
        return HookExecutor.ExecuteAsynchronousAfterTestSessionHook(MethodInfo, context,
            () => Body!.Invoke(context, cancellationToken)
        );
    }
}