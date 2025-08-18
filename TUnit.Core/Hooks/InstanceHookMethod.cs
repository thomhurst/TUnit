using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Extensions;
using TUnit.Core.Interfaces;

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

    public Func<object, TestContext, CancellationToken, ValueTask>? Body { get; init; }

    public ValueTask ExecuteAsync(TestContext context, CancellationToken cancellationToken)
    {
        // Skip instance hooks if this is a pre-skipped test
        if (context.TestDetails.ClassInstance is SkippedTestInstance || !string.IsNullOrEmpty(context.SkipReason) || context.InternalExecutableTest.State is TestState.Skipped)
        {
            return new ValueTask();
        }

        // If the instance is still a placeholder, we can't execute instance hooks
        if (context.TestDetails.ClassInstance is PlaceholderInstance)
        {
            throw new InvalidOperationException($"Cannot execute instance hook {Name} because the test instance has not been created yet. This is likely a framework bug.");
        }

        return HookExecutor.ExecuteBeforeTestHook(MethodInfo, context,
            () => Body!.Invoke(context.TestDetails.ClassInstance, context, cancellationToken)
        );
    }
}
