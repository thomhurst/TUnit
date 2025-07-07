using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Extensions;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Hooks;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public abstract record StaticHookMethod<T> : StaticHookMethod, IExecutableHook<T>
{
    public Func<T, CancellationToken, ValueTask>? Body { get; init; }
    public abstract ValueTask ExecuteAsync(T context, CancellationToken cancellationToken);
}

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public abstract record StaticHookMethod
{
    public required MethodMetadata MethodInfo { get; init; }

    [field: AllowNull, MaybeNull]
    public string Name => field ??= $"{MethodInfo.Class.Type.Name}.{MethodInfo.Name}({string.Join(", ", MethodInfo.Parameters.Select(x => x.Name))})";

    public Type ClassType => MethodInfo.Class.Type;
    public Assembly? Assembly => ClassType?.Assembly;

    [field: AllowNull, MaybeNull]
    public IEnumerable<Attribute> Attributes => field ??= MethodInfo.GetCustomAttributes();

    public TAttribute? GetAttribute<TAttribute>() where TAttribute : Attribute => Attributes.OfType<TAttribute>().FirstOrDefault();

    // TODO?
    public TimeSpan? Timeout => GetAttribute<TimeoutAttribute>()?.Timeout;

    public required IHookExecutor HookExecutor { get; init; }

    public required int Order { get; init; }

    public required string FilePath { get; init; }

    public required int LineNumber { get; init; }
}
