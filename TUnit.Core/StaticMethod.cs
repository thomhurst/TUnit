using System.Reflection;

namespace TUnit.Core;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public record StaticMethod
{
    public required MethodInfo MethodInfo { get; init; }
    public required Func<CancellationToken, Task> Body { get; init; }
}

public record StaticMethod<T>
{
    public required MethodInfo MethodInfo { get; init; }
    public required Func<T, CancellationToken, Task> Body { get; init; }
}