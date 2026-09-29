using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core.Hooks;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public record InstanceHookMethod : HookMethod, IExecutableHook<TestContext>
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    private readonly Type _classType = null!;

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    public override Type ClassType => _classType;

    public required Type InitClassType
    {
        [param: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        init { _classType = value; }
    }

    /// <summary>
    /// The hook body. Source-generated bodies call the hook method directly, without an async state machine,
    /// so invoke it only through <see cref="HookBodyInvoker"/> to keep async-method semantics.
    /// </summary>
    public Func<object, TestContext, CancellationToken, ValueTask>? Body { get; init; }

    public ValueTask ExecuteAsync(TestContext context, CancellationToken cancellationToken)
    {
        // Skip instance hooks if this is a pre-skipped test
        if (context.Metadata.TestDetails.ClassInstance is SkippedTestInstance)
        {
            return new ValueTask();
        }

        // If the instance is still a placeholder, we can't execute instance hooks
        if (context.Metadata.TestDetails.ClassInstance is PlaceholderInstance)
        {
            throw new InvalidOperationException($"Cannot execute instance hook {Name} because the test instance has not been created yet. This is likely a framework bug.");
        }

        return ResolveEffectiveExecutor(context).ExecuteBeforeTestHook(MethodInfo, context,
            () => HookBodyInvoker.InvokeAsync(Body!, context.Metadata.TestDetails.ClassInstance, context, cancellationToken)
        );
    }
}
