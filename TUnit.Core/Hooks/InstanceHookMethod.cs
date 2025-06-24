using System.Diagnostics.CodeAnalysis;
using System.Reflection;
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
    public string Name =>  field ??= $"{ClassType.Name}.{MethodInfo.Name}({string.Join(", ", MethodInfo.Parameters.Select(x => x.Name))})";

    public Attribute[] MethodAttributes => MethodInfo.Attributes.Select(a => a.Instance).ToArray();
    public Attribute[] ClassAttributes => MethodInfo.Class.Attributes.Select(a => a.Instance).ToArray();
    public Attribute[] AssemblyAttributes => MethodInfo.Class.Assembly.Attributes.Select(a => a.Instance).ToArray();
    
    [field: AllowNull, MaybeNull]
    public IEnumerable<Attribute> Attributes => field ??=
        [..MethodAttributes, ..ClassAttributes, ..AssemblyAttributes];

    public TAttribute? GetAttribute<TAttribute>() where TAttribute : Attribute => Attributes.OfType<TAttribute>().FirstOrDefault();

    public TimeSpan? Timeout => GetAttribute<TimeoutAttribute>()?.Timeout;
    
    public required IHookExecutor HookExecutor { get; init; }
    
    public required int Order { get; init; }
    
    public Func<object, TestContext, CancellationToken, ValueTask>? Body { get; init; }

    public ValueTask ExecuteAsync(TestContext context, CancellationToken cancellationToken)
    {
        return HookExecutor.ExecuteBeforeTestHook(MethodInfo, context,
            () => Body!.Invoke(context.TestDetails?.ClassInstance ?? throw new InvalidOperationException("ClassInstance is null"), context, cancellationToken)
        );
    }
}