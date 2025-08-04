using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Extensions;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Hooks;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public record InstanceHookMethod : IExecutableHook<TestContext>
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    public required Type ClassType { get; init; }
    public Assembly Assembly => ClassType.Assembly;
    public required MethodMetadata MethodInfo { get; init; }

    [field: AllowNull, MaybeNull]
    public string Name => field ??= $"{ClassType.Name}.{MethodInfo.Name}({string.Join(", ", MethodInfo.Parameters.Select(x => x.Name))})";

    [field: AllowNull, MaybeNull] public IEnumerable<Attribute> Attributes => field ??= MethodInfo.GetCustomAttributes();

    public TAttribute? GetAttribute<TAttribute>() where TAttribute : Attribute => Attributes.OfType<TAttribute>().FirstOrDefault();

    public TimeSpan? Timeout => GetAttribute<TimeoutAttribute>()?.Timeout;

    public required IHookExecutor HookExecutor { get; init; }

    public required int Order { get; init; }
    
    public required int RegistrationIndex { get; init; }

    public Func<object, TestContext, CancellationToken, ValueTask>? Body { get; init; }

    public ValueTask ExecuteAsync(TestContext context, CancellationToken cancellationToken)
    {
        // Skip instance hooks if this is a pre-skipped test
        if (context.TestDetails.ClassInstance is SkippedTestInstance)
        {
            return new ValueTask();
        }

        return HookExecutor.ExecuteBeforeTestHook(MethodInfo, context,
            () => Body!.Invoke(context.TestDetails.ClassInstance, context, cancellationToken)
        );
    }
}
