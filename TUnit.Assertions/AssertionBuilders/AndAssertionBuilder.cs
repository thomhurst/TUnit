using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertionBuilders;

public class AndAssertionBuilder<TActual, TAnd, TOr> : AssertionBuilder<TActual, TAnd, TOr>, IAndAssertionBuilder 
    where TOr : IOr<TActual, TAnd, TOr> where TAnd : IAnd<TActual, TAnd, TOr>
{
    public AndAssertionBuilder(Func<Task<AssertionData<TActual>>> assertionDataDelegate) : base(assertionDataDelegate)
    {
    }

    public AndAssertionBuilder(Func<Task<AssertionData<TActual>>> assertionDataDelegate, string actual) : base(assertionDataDelegate, actual)
    {
    }
}