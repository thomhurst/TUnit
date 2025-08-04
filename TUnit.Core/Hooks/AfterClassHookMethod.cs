namespace TUnit.Core.Hooks;

public record AfterClassHookMethod : StaticHookMethod<ClassHookContext>
{
    public override ValueTask ExecuteAsync(ClassHookContext context, CancellationToken cancellationToken)
    {
        return this.HookExecutor.ExecuteAfterClassHook(MethodInfo, context,
            () => Body!.Invoke(context, cancellationToken)
        );
    }
}
