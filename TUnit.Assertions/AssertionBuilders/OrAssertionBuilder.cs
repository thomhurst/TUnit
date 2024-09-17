using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertionBuilders;

public class OrAssertionBuilder<TActual, TAnd, TOr> : AssertionBuilder<TActual, TAnd, TOr>, IOrAssertionBuilder 
    where TOr : IOr<TActual, TAnd, TOr> where TAnd : IAnd<TActual, TAnd, TOr>
{
    public OrAssertionBuilder(Func<Task<AssertionData<TActual>>> assertionDataDelegate) : base(assertionDataDelegate)
    {
    }

    public OrAssertionBuilder(Func<Task<AssertionData<TActual>>> assertionDataDelegate, string actual) : base(assertionDataDelegate, actual)
    {
    }
}