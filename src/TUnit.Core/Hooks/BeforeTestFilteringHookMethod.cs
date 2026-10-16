namespace TUnit.Core.Hooks;

/// <summary>
/// A <c>[Before(TestFiltering)]</c> hook that can still exclude, skip and order the tests selected for the run.
/// </summary>
public record BeforeTestFilteringHookMethod : StaticHookMethod<TestFilteringContext>
{
    /// <inheritdoc />
    public override ValueTask ExecuteAsync(TestFilteringContext context, CancellationToken cancellationToken)
    {
        return HookExecutor.ExecuteBeforeTestFilteringHook(MethodInfo, context,
            () => InvokeBodyAsync(context, cancellationToken)
        );
    }
}
