namespace TUnit.Assertions.AssertConditions.Operators;

public interface IOr<out TSelf, TActual, TAnd, TOr>
    where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
    where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
{
    public static abstract TSelf Create(BaseAssertCondition<TActual, TAnd, TOr> otherAssertCondition);
}