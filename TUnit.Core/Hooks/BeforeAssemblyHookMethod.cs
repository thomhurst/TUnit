namespace TUnit.Core.Hooks;

public record BeforeAssemblyHookMethod : StaticHookMethod<AssemblyHookContext>
{
    public override ValueTask ExecuteAsync(AssemblyHookContext context, CancellationToken cancellationToken)
    {
        return this.HookExecutor.ExecuteBeforeAssemblyHook(MethodInfo, context,
            () => Body!.Invoke(context, cancellationToken)
        );
    }
}
