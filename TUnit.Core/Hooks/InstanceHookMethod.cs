﻿using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Hooks;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public record InstanceHookMethod : IExecutableHook<TestContext>
{
    public required Type ClassType { get; init; }
    public Assembly Assembly => ClassType.Assembly;
    public required SourceGeneratedMethodInformation MethodInfo { get; init; }
    
    [field: AllowNull, MaybeNull]
    public string Name =>  field ??= $"{ClassType.Name}.{MethodInfo.Name}({string.Join(", ", MethodInfo.Parameters.Select(x => x.Name))})";

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
    
    public Func<object, TestContext, CancellationToken, Task>? AsyncBody { get; init; }
    public Action<object, TestContext, CancellationToken>? Body { get; init; }
    
    public bool IsSynchronous => Body != null;
    
    public bool Execute(TestContext context, CancellationToken cancellationToken)
    {
        if (Body != null)
        {
            HookExecutor.ExecuteSynchronousBeforeTestHook(MethodInfo, context,
                () => Body.Invoke(context.TestDetails.ClassInstance!, context, cancellationToken)
            );
            return true;
        }

        return false;
    }

    public Task ExecuteAsync(TestContext context, CancellationToken cancellationToken)
    {
        return HookExecutor.ExecuteAsynchronousBeforeTestHook(MethodInfo, context,
            () => AsyncBody!.Invoke(context.TestDetails.ClassInstance!, context, cancellationToken)
        );
    }
}