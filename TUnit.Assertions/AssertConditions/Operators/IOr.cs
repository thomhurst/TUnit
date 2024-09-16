using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public interface IOr<TActual, TAnd, TOr>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    public static abstract TOr Create(Func<Task<AssertionData<TActual>>> assertionDataDelegate, AssertionBuilder<TActual, TAnd, TOr> assertionBuilder);
}