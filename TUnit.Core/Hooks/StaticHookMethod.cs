﻿using System.Diagnostics.CodeAnalysis;

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
public abstract record StaticHookMethod : HookMethod
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    public override Type ClassType => MethodInfo.Class.Type;

    public required string FilePath { get; init; }

    public required int LineNumber { get; init; }
}
