namespace TUnit.Core.Hooks;

public record AfterTestHookMethod : StaticHookMethod<TestContext>
{
    public override ValueTask ExecuteAsync(TestContext context, CancellationToken cancellationToken)
    {
        // Skip AfterEvery hooks if this is a skipped test
        if (context.TestDetails.ClassInstance is SkippedTestInstance || !string.IsNullOrEmpty(context.SkipReason) || context.InternalExecutableTest.State is TestState.Skipped)
        {
            return new ValueTask();
        }

        return HookExecutor.ExecuteAfterTestHook(MethodInfo, context,
            () => Body!.Invoke(context, cancellationToken)
        );
    }
}
