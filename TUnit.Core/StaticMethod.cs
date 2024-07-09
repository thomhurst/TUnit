using System.Reflection;
using TUnit.Core.Helpers;

namespace TUnit.Core;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public record StaticMethod
{
    public required MethodInfo MethodInfo { get; init; }
    public required Func<CancellationToken, Task> Body { get; init; }
    
    public Type ClassType => MethodInfo.ReflectedType!;
    public Assembly Assembly => ClassType.Assembly;

    private IEnumerable<Attribute>? _attributes;

    public IEnumerable<Attribute> Attributes => _attributes ??=
        [..MethodInfo.GetCustomAttributes(), ..ClassType.GetCustomAttributes(), ..Assembly.GetCustomAttributes()];

    public TAttribute? GetAttribute<TAttribute>() where TAttribute : Attribute => AttributeHelper.GetAttribute<TAttribute>(Attributes);

    public TimeSpan? Timeout => GetAttribute<TimeoutAttribute>()?.Timeout;
}

public record StaticMethod<T>
{
    public required MethodInfo MethodInfo { get; init; }
    public required Func<T, CancellationToken, Task> Body { get; init; }
    
    public Type ClassType => MethodInfo.ReflectedType!;
    public Assembly Assembly => ClassType.Assembly;

    private IEnumerable<Attribute>? _attributes;

    public IEnumerable<Attribute> Attributes => _attributes ??=
        [..MethodInfo.GetCustomAttributes(), ..ClassType.GetCustomAttributes(), ..Assembly.GetCustomAttributes()];

    public TAttribute? GetAttribute<TAttribute>() where TAttribute : Attribute => AttributeHelper.GetAttribute<TAttribute>(Attributes);

    public TimeSpan? Timeout => GetAttribute<TimeoutAttribute>()?.Timeout;
}