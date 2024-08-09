using TUnit.Core.Interfaces;

namespace TUnit.Core.Executors;

public abstract class HookExecutorAttribute : TUnitAttribute
{
    public abstract Type HookExecutorType { get; }
}

public class HookExecutorAttribute<T> : HookExecutorAttribute where T : IHookExecutor, new()
{
    public override Type HookExecutorType { get; } = typeof(T);
}