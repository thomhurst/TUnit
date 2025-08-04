namespace TUnit.Core.Hooks;

public record AfterAssemblyHookMethod : StaticHookMethod<AssemblyHookContext>
{
    public override ValueTask ExecuteAsync(AssemblyHookContext context, CancellationToken cancellationToken)
    {
        return this.HookExecutor.ExecuteAfterAssemblyHook(MethodInfo, context,
            () => Body!.Invoke(context, cancellationToken)
        );
    }
}
