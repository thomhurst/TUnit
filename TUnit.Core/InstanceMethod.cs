using System.Reflection;

namespace TUnit.Core;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public record InstanceMethod<TClassType>
{
    public required MethodInfo MethodInfo { get; init; }
    public required Func<TClassType, CancellationToken, Task> Body { get; init; }
}