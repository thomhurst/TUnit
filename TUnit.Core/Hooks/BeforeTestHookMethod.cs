namespace TUnit.Core.Hooks;

public record BeforeTestHookMethod : StaticHookMethod<TestContext>
{
    public override bool Execute(TestContext context, CancellationToken cancellationToken)
    {
        if (Body != null)
        {
            HookExecutor.ExecuteSynchronousBeforeTestHook(MethodInfo, context,
                () => Body.Invoke(context, cancellationToken)
            );
            return true;
        }

        return false;
    }

    public override Task ExecuteAsync(TestContext context, CancellationToken cancellationToken)
    {
        return HookExecutor.ExecuteAsynchronousBeforeTestHook(MethodInfo, context,
            () => Body!.Invoke(context, cancellationToken)
        );
    }
}