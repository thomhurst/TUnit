namespace TUnit.Assertions.AssertConditions.Operators;

public abstract class And<TActual, TAnd, TOr>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    protected readonly BaseAssertCondition<TActual, TAnd, TOr> OtherAssertCondition;

    public And(BaseAssertCondition<TActual, TAnd, TOr> otherAssertCondition)
    {
        OtherAssertCondition = otherAssertCondition;
    }
}