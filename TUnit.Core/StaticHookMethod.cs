using System.Reflection;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public record StaticHookMethod<T>
{
    public required MethodInfo MethodInfo { get; init; }
    public required Func<T, CancellationToken, Task> Body { get; init; }
    private string? _name;
    public string Name =>  _name ??=$"{ClassType.Name}.{MethodInfo.Name}({string.Join(", ", MethodInfo.GetParameters().Select(x => x.ParameterType.Name))})";
    public Type ClassType => MethodInfo.ReflectedType!;
    public Assembly Assembly => ClassType.Assembly;

    private IEnumerable<Attribute>? _attributes;

    public IEnumerable<Attribute> Attributes => _attributes ??=
        [..MethodInfo.GetCustomAttributes(), ..ClassType.GetCustomAttributes(), ..Assembly.GetCustomAttributes()];

    public TAttribute? GetAttribute<TAttribute>() where TAttribute : Attribute => AttributeHelper.GetAttribute<TAttribute>(Attributes);

    public TimeSpan? Timeout => GetAttribute<TimeoutAttribute>()?.Timeout;
    
    public required IHookExecutor HookExecutor { get; init; }
}