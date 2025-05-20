using TUnit.Core.Interfaces;

namespace TUnit.Core.Executors;

public class HookExecutorAttribute(Type type) : TUnitAttribute
{
    public Type HookExecutorType { get; } = type;
}

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
public sealed class HookExecutorAttribute<T>() : HookExecutorAttribute(typeof(T)) where T : IHookExecutor, new();