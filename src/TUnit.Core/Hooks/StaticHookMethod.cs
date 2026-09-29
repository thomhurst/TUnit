using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core.Hooks;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public abstract record StaticHookMethod<T> : StaticHookMethod, IExecutableHook<T>
{
    /// <summary>
    /// The hook body. Source-generated bodies call the hook method directly, without an async state machine,
    /// so invoke it only through <see cref="InvokeBodyAsync"/> to keep async-method semantics.
    /// </summary>
    public Func<T, CancellationToken, ValueTask>? Body { get; init; }
    public abstract ValueTask ExecuteAsync(T context, CancellationToken cancellationToken);

    internal ValueTask InvokeBodyAsync(T context, CancellationToken cancellationToken)
        => HookBodyInvoker.InvokeAsync(Body!, context, cancellationToken);
}

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public abstract record StaticHookMethod : HookMethod
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    public override Type ClassType => MethodInfo.Class.Type;

    public required string FilePath { get; init; }

    public required int LineNumber { get; init; }
}
