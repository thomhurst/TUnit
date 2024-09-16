using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public interface IAnd<TActual, TAnd, TOr>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    public static abstract TAnd Create(Func<Task<AssertionData<TActual>>> assertionDataDelegate, AssertionBuilder<TActual, TAnd, TOr> assertionBuilder);
}