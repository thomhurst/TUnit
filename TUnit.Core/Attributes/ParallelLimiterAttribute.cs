using TUnit.Core.Interfaces;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ParallelLimiterAttribute<TParallelLimit> : TUnitAttribute, ITestRegisteredEvents
    where TParallelLimit : IParallelLimit, new()
{
    public ValueTask OnTestRegistered(TestRegisterContext testRegisterContext)
    {
        testRegisterContext.SetParallelLimiter(new TParallelLimit());
        return ValueTask.CompletedTask;
    }
};