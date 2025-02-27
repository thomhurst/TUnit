namespace TUnit.Core.Hooks;

public record AfterTestHookMethod : StaticHookMethod<TestContext>
{
    public override bool Execute(TestContext context, CancellationToken cancellationToken)
    {
        if (Body != null)
        {
            HookExecutor.ExecuteSynchronousAfterTestHook(MethodInfo, context,
                () => Body.Invoke(context, cancellationToken)
            );
            return true;
        }

        return false;
    }

    public override Task ExecuteAsync(TestContext context, CancellationToken cancellationToken)
    {
        return HookExecutor.ExecuteAsynchronousAfterTestHook(MethodInfo, context,
            () => Body!.Invoke(context, cancellationToken)
        );
    }
}