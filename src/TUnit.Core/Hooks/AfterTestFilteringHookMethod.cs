namespace TUnit.Core.Hooks;

/// <summary>
/// A <c>[After(TestFiltering)]</c> hook that sees the final set of tests selected for the run.
/// </summary>
public record AfterTestFilteringHookMethod : StaticHookMethod<TestFilteringContext>
{
    /// <inheritdoc />
    public override ValueTask ExecuteAsync(TestFilteringContext context, CancellationToken cancellationToken)
    {
        return HookExecutor.ExecuteAfterTestFilteringHook(MethodInfo, context,
            () => InvokeBodyAsync(context, cancellationToken)
        );
    }
}
