using System.Reflection;
using TUnit.Core;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;

namespace TUnit.Engine;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public record InstanceHookMethod<TClassType>
{
    public Type ClassType { get; } = typeof(TClassType);
    public Assembly Assembly { get; } = typeof(TClassType).Assembly;
    public required MethodInfo MethodInfo { get; init; }
    private string? _name;
    public string Name =>  _name ??= $"{ClassType.Name}.{MethodInfo.Name}({string.Join(", ", MethodInfo.GetParameters().Select(x => x.ParameterType.Name))})";
    public required Func<TClassType, TestContext, CancellationToken, Task> Body { get; init; }

    private IEnumerable<Attribute>? _attributes;

    public IEnumerable<Attribute> Attributes => _attributes ??=
        [..MethodInfo.GetCustomAttributes(), ..ClassType.GetCustomAttributes(), ..Assembly.GetCustomAttributes()];

    public TAttribute? GetAttribute<TAttribute>() where TAttribute : Attribute => AttributeHelper.GetAttribute<TAttribute>(Attributes);

    public TimeSpan? Timeout => GetAttribute<TimeoutAttribute>()?.Timeout;
    
    public required IHookExecutor HookExecutor { get; init; }
}