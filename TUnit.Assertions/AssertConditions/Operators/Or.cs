namespace TUnit.Assertions.AssertConditions.Operators;

public class Or<TActual, TAnd, TOr>
    where TAnd : And<TActual?, TAnd, TOr>, IAnd<TAnd, TActual?, TAnd, TOr>
    where TOr : Or<TActual?, TAnd, TOr>, IOr<TOr, TActual?, TAnd, TOr>
{
    protected readonly BaseAssertCondition<TActual?, TAnd, TOr> OtherAssertCondition;

    public Or(BaseAssertCondition<TActual?, TAnd, TOr> otherAssertCondition)
    {
        OtherAssertCondition = otherAssertCondition;
    }
}