using TUnit.Core.Interfaces;

namespace TUnit.Core.Executors;

public abstract class TestExecutorAttribute : TUnitAttribute
{
    public abstract Type TestExecutorType { get; }

    internal TestExecutorAttribute()
    {
    }
}

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
public sealed class TestExecutorAttribute<T> : TestExecutorAttribute where T : ITestExecutor, new()
{
    public override Type TestExecutorType { get; } = typeof(T);
}