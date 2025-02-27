namespace TUnit.Core.Hooks;

public record AfterClassHookMethod : StaticHookMethod<ClassHookContext>
{
    public override bool Execute(ClassHookContext context, CancellationToken cancellationToken)
    {
        if (Body != null)
        {
            HookExecutor.ExecuteSynchronousAfterClassHook(MethodInfo, context,
                () => Body.Invoke(context, cancellationToken)
            );
            return true;
        }

        return false;
    }

    public override Task ExecuteAsync(ClassHookContext context, CancellationToken cancellationToken)
    {
        return HookExecutor.ExecuteAsynchronousAfterClassHook(MethodInfo, context,
            () => Body!.Invoke(context, cancellationToken)
        );
    }
}