using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Interfaces;

public interface IOutputsChain<out TAssertionBuilder, TActual> 
    where TAssertionBuilder : AssertionBuilder<TActual>
{
    public static abstract TAssertionBuilder Create(Func<Task<AssertionData<TActual>>> assertionDataDelegate, AssertionBuilder<TActual> assertionBuilder);
}