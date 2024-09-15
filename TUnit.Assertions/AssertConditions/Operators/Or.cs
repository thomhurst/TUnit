namespace TUnit.Assertions.AssertConditions.Operators;

public class Or<TActual, TAnd, TOr>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    protected readonly BaseAssertCondition<TActual, TAnd, TOr> OtherAssertCondition;

    public Or(BaseAssertCondition<TActual, TAnd, TOr> otherAssertCondition)
    {
        OtherAssertCondition = otherAssertCondition;
    }
}