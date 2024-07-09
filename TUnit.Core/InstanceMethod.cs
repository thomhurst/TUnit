using System.Reflection;
using TUnit.Core.Helpers;

namespace TUnit.Core;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public record InstanceMethod<TClassType>
{
    public Type ClassType { get; } = typeof(TClassType);
    public Assembly Assembly { get; } = typeof(TClassType).Assembly;
    public required MethodInfo MethodInfo { get; init; }
    public required Func<TClassType, CancellationToken, Task> Body { get; init; }

    private IEnumerable<Attribute>? _attributes;

    public IEnumerable<Attribute> Attributes => _attributes ??=
        [..MethodInfo.GetCustomAttributes(), ..ClassType.GetCustomAttributes(), ..Assembly.GetCustomAttributes()];

    public TAttribute? GetAttribute<TAttribute>() where TAttribute : Attribute => AttributeHelper.GetAttribute<TAttribute>(Attributes);

    public TimeSpan? Timeout => GetAttribute<TimeoutAttribute>()?.Timeout;
}