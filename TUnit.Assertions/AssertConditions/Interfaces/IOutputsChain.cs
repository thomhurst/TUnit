using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Interfaces;

public interface IOutputsChain<TAssertionBuilder, TActual, TAnd, TOr> 
    where TAssertionBuilder : AssertionBuilder<TActual, TAnd, TOr>
    where TAnd : IAnd<TActual, TAnd, TOr> 
    where TOr : IOr<TActual, TAnd, TOr>
{
    public static abstract TAssertionBuilder Create(Func<Task<AssertionData<TActual>>> assertionDataDelegate, AssertionBuilder<TActual, TAnd, TOr> assertionBuilder);
}