namespace TUnit.Core.Hooks;

public record BeforeClassHookMethod : StaticHookMethod<ClassHookContext>
{
    public override bool Execute(ClassHookContext context, CancellationToken cancellationToken)
    {
        if (Body != null)
        {
            HookExecutor.ExecuteSynchronousBeforeClassHook(MethodInfo, context,
                () => Body.Invoke(context, cancellationToken)
            );
            return true;
        }

        return false;
    }

    public override Task ExecuteAsync(ClassHookContext context, CancellationToken cancellationToken)
    {
        return HookExecutor.ExecuteAsynchronousBeforeClassHook(MethodInfo, context,
            () => Body!.Invoke(context, cancellationToken)
        );
    }
}