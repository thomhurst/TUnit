﻿using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Hooks;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public record InstanceHookMethod<TClassType> : InstanceHookMethod, IExecutableHook<TestContext>
{
    public override Type ClassType { get; } = typeof(TClassType);
    public override Assembly Assembly { get; } = typeof(TClassType).Assembly;
    
    public Func<TClassType, TestContext, CancellationToken, Task>? AsyncBody { get; init; }
    public Action<TClassType, TestContext, CancellationToken>? Body { get; init; }
    
    public override bool IsSynchronous => Body != null;
    public bool Execute(TestContext context, CancellationToken cancellationToken)
    {
        if (Body != null)
        {
            HookExecutor.ExecuteSynchronousBeforeTestHook(MethodInfo, context,
                () => Body.Invoke((TClassType)context.TestDetails.ClassInstance!, context, cancellationToken)
            );
            return true;
        }

        return false;
    }

    public Task ExecuteAsync(TestContext context, CancellationToken cancellationToken)
    {
        return HookExecutor.ExecuteAsynchronousBeforeTestHook(MethodInfo, context,
            () => AsyncBody!.Invoke((TClassType)context.TestDetails.ClassInstance!, context, cancellationToken)
        );
    }
}

public abstract record InstanceHookMethod
{
    public abstract Type ClassType { get; }
    public abstract Assembly Assembly { get; }
    public required MethodInfo MethodInfo { get; init; }
    
    [field: AllowNull, MaybeNull]
    public string Name =>  field ??= $"{ClassType.Name}.{MethodInfo.Name}({string.Join(", ", MethodInfo.GetParameters().Select(x => x.ParameterType.Name))})";

    [field: AllowNull, MaybeNull]
    public IEnumerable<Attribute> Attributes => field ??=
        [..MethodInfo.GetCustomAttributes(), ..ClassType.GetCustomAttributes(), ..Assembly.GetCustomAttributes()];

    public TAttribute? GetAttribute<TAttribute>() where TAttribute : Attribute => AttributeHelper.GetAttribute<TAttribute>(Attributes);

    public TimeSpan? Timeout => GetAttribute<TimeoutAttribute>()?.Timeout;
    
    public required IHookExecutor HookExecutor { get; init; }
    
    public required int Order { get; init; }
    
    public abstract bool IsSynchronous { get; }
}