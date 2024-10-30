using TUnit.Core.Interfaces;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ParallelLimiterAttribute<TParallelLimit> : TUnitAttribute, ITestRegisteredEventReceiver
    where TParallelLimit : IParallelLimit, new()
{
    public ValueTask OnTestRegistered(TestRegisteredContext testRegisteredContext)
    {
        testRegisteredContext.SetParallelLimiter(new TParallelLimit());
        return ValueTask.CompletedTask;
    }
};