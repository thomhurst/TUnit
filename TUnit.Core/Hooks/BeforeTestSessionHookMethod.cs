namespace TUnit.Core.Hooks;

public record BeforeTestSessionHookMethod : StaticHookMethod<TestSessionContext>
{
    public override bool Execute(TestSessionContext context, CancellationToken cancellationToken)
    {
        if (Body != null)
        {
            HookExecutor.ExecuteSynchronousBeforeTestSessionHook(MethodInfo, context,
                () => Body.Invoke(context, cancellationToken)
            );
            return true;
        }

        return false;
    }

    public override Task ExecuteAsync(TestSessionContext context, CancellationToken cancellationToken)
    {
        return HookExecutor.ExecuteAsynchronousBeforeTestSessionHook(MethodInfo, context,
            () => Body!.Invoke(context, cancellationToken)
        );
    }
}