using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Hooks;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public abstract record StaticHookMethod<T> : StaticHookMethod, IExecutableHook<T>
{
    public Func<T, CancellationToken, Task>? AsyncBody { get; init; }
    public Action<T, CancellationToken>? Body { get; init; }

    public abstract bool Execute(T context, CancellationToken cancellationToken);
    public abstract Task ExecuteAsync(T context, CancellationToken cancellationToken);
    public bool IsSynchronous => Body != null;
}

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public abstract record StaticHookMethod
{
    public required MethodInfo MethodInfo { get; init; }

    [field: AllowNull, MaybeNull]
    public string Name =>  field ??= $"{ClassType.Name}.{MethodInfo.Name}({string.Join(", ", MethodInfo.GetParameters().Select(x => x.ParameterType.Name))})";
    public Type ClassType => MethodInfo.ReflectedType!;
    public Assembly Assembly => ClassType.Assembly;
    
    public required Attribute[] MethodAttributes { get; init; }
    public required Attribute[] ClassAttributes { get; init; }
    public required Attribute[] AssemblyAttributes { get; init; }

    [field: AllowNull, MaybeNull]
    public IEnumerable<Attribute> Attributes => field ??=
        [..MethodAttributes, ..ClassAttributes, ..AssemblyAttributes];

    public TAttribute? GetAttribute<TAttribute>() where TAttribute : Attribute => Attributes.OfType<TAttribute>().FirstOrDefault();

    public TimeSpan? Timeout => GetAttribute<TimeoutAttribute>()?.Timeout;
    
    public required IHookExecutor HookExecutor { get; init; }
    
    public required int Order { get; init; }

    public required string FilePath { get; init; }
    
    public required int LineNumber { get; init; }
}