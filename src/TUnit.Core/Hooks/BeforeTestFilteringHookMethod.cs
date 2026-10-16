namespace TUnit.Core.Hooks;

public record BeforeTestFilteringHookMethod : StaticHookMethod<TestFilteringContext>
{
    public override ValueTask ExecuteAsync(TestFilteringContext context, CancellationToken cancellationToken)
    {
        return HookExecutor.ExecuteBeforeTestFilteringHook(MethodInfo, context,
            () => InvokeBodyAsync(context, cancellationToken)
        );
    }
}
