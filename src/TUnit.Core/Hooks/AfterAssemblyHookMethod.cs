namespace TUnit.Core.Hooks;

public record AfterAssemblyHookMethod : StaticHookMethod<AssemblyHookContext>
{
    public override ValueTask ExecuteAsync(AssemblyHookContext context, CancellationToken cancellationToken)
    {
        return HookExecutor.ExecuteAfterAssemblyHook(MethodInfo, context,
            () => InvokeBodyAsync(context, cancellationToken)
        );
    }
}
