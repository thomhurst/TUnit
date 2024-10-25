using System.Reflection;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public record InstanceHookMethod<TClassType> : InstanceHookMethod
{
    public override Type ClassType { get; } = typeof(TClassType);
    public override Assembly Assembly { get; } = typeof(TClassType).Assembly;
    
    public override Task ExecuteHook(TestContext testContext, CancellationToken cancellationToken)
    {
        return Body.Invoke((TClassType)testContext.TestDetails.ClassInstance!, testContext, cancellationToken);
    }

    public required Func<TClassType, TestContext, CancellationToken, Task> Body { get; init; }
}

public abstract record InstanceHookMethod
{
    public abstract Type ClassType { get; }
    public abstract Assembly Assembly { get; }
    public required MethodInfo MethodInfo { get; init; }
    private string? _name;
    public string Name =>  _name ??= $"{ClassType.Name}.{MethodInfo.Name}({string.Join(", ", MethodInfo.GetParameters().Select(x => x.ParameterType.Name))})";

    private IEnumerable<Attribute>? _attributes;

    public IEnumerable<Attribute> Attributes => _attributes ??=
        [..MethodInfo.GetCustomAttributes(), ..ClassType.GetCustomAttributes(), ..Assembly.GetCustomAttributes()];

    public TAttribute? GetAttribute<TAttribute>() where TAttribute : Attribute => AttributeHelper.GetAttribute<TAttribute>(Attributes);

    public TimeSpan? Timeout => GetAttribute<TimeoutAttribute>()?.Timeout;
    
    public required IHookExecutor HookExecutor { get; init; }
    
    public required int Order { get; init; }
    
    public abstract Task ExecuteHook(TestContext testContext, CancellationToken cancellationToken);
}