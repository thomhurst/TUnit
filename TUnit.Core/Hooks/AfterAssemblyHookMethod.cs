namespace TUnit.Core.Hooks;

public record AfterAssemblyHookMethod : StaticHookMethod<AssemblyHookContext>
{
    public override bool Execute(AssemblyHookContext context, CancellationToken cancellationToken)
    {
        if (Body != null)
        {
            HookExecutor.ExecuteSynchronousAfterAssemblyHook(MethodInfo, context,
                () => Body.Invoke(context, cancellationToken)
            );
            return true;
        }

        return false;
    }

    public override Task ExecuteAsync(AssemblyHookContext context, CancellationToken cancellationToken)
    {
        return HookExecutor.ExecuteAsynchronousAfterAssemblyHook(MethodInfo, context,
            () => Body!.Invoke(context, cancellationToken)
        );
    }
}