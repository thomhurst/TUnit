namespace TUnit.Core.Hooks;

public record AfterTestFilteringHookMethod : StaticHookMethod<TestFilteringContext>
{
    public override ValueTask ExecuteAsync(TestFilteringContext context, CancellationToken cancellationToken)
    {
        return HookExecutor.ExecuteAfterTestFilteringHook(MethodInfo, context,
            () => InvokeBodyAsync(context, cancellationToken)
        );
    }
}
