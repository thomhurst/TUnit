using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

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

    /// <summary>
    /// The base method definition for this hook (i.e. <see cref="MethodInfo.GetBaseDefinition"/>),
    /// used by the engine to deduplicate virtual hook methods that are overridden in a derived
    /// class. Optional — when null, the engine resolves it via reflection from <see cref="ClassType"/>
    /// and the metadata name/parameters. Set directly by the reflection discovery path which already
    /// holds a <see cref="System.Reflection.MethodInfo"/> at registration time.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public MethodInfo? BaseDefinition { get; init; }

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

        return HookExecutor.ExecuteBeforeTestHook(MethodInfo, context,
            () => Body!.Invoke(context.Metadata.TestDetails.ClassInstance, context, cancellationToken)
        );
    }
}
