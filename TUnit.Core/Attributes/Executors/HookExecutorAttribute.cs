using TUnit.Core.Interfaces;

namespace TUnit.Core.Executors;

public abstract class HookExecutorAttribute : TUnitAttribute
{
    public abstract Type HookExecutorType { get; }

    internal HookExecutorAttribute()
    {
    }
}

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
public sealed class HookExecutorAttribute<T> : HookExecutorAttribute where T : IHookExecutor, new()
{
    public override Type HookExecutorType { get; } = typeof(T);
}