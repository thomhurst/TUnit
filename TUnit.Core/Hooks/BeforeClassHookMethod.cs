namespace TUnit.Core.Hooks;

public record BeforeClassHookMethod : StaticHookMethod<ClassHookContext>
{
    public override ValueTask ExecuteAsync(ClassHookContext context, CancellationToken cancellationToken)
    {
        return base.HookExecutor.ExecuteBeforeClassHook(MethodInfo, context,
            () => Body!.Invoke(context, cancellationToken)
        );
    }
}
