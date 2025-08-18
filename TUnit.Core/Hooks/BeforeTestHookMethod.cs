namespace TUnit.Core.Hooks;

public record BeforeTestHookMethod : StaticHookMethod<TestContext>
{
    public override ValueTask ExecuteAsync(TestContext context, CancellationToken cancellationToken)
    {
        // Skip BeforeEvery hooks if this is a skipped test
        if (context.TestDetails.ClassInstance is SkippedTestInstance || !string.IsNullOrEmpty(context.SkipReason))
        {
            return new ValueTask();
        }

        return HookExecutor.ExecuteBeforeTestHook(MethodInfo, context,
            () => Body!.Invoke(context, cancellationToken)
        );
    }
}
