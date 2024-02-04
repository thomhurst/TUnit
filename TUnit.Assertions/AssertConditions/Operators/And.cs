namespace TUnit.Assertions.AssertConditions.Operators;

public abstract class And<TActual, TAnd, TOr>
    where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
    where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
{
    protected readonly BaseAssertCondition<TActual, TAnd, TOr> OtherAssertCondition;

    public And(BaseAssertCondition<TActual, TAnd, TOr> otherAssertCondition)
    {
        OtherAssertCondition = otherAssertCondition;
    }
}