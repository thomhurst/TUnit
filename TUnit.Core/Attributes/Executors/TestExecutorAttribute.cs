using TUnit.Core.Interfaces;

namespace TUnit.Core.Executors;

public abstract class TestExecutorAttribute : TUnitAttribute
{
    public abstract Type TestExecutorType { get; }
}

public class TestExecutorAttribute<T> : TestExecutorAttribute where T : ITestExecutor, new()
{
    public override Type TestExecutorType { get; } = typeof(T);
}