using TUnit.Core.Interfaces;

namespace TUnit.Core.Executors;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
public sealed class TestExecutorAttribute<T> : TUnitAttribute, ITestRegisteredEventReceiver where T : ITestExecutor, new()
{
    public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        context.SetTestExecutor(new T());
        return ValueTask.CompletedTask;
    }
}