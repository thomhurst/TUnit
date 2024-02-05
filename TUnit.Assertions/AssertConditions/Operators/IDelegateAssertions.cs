namespace TUnit.Assertions.AssertConditions.Operators;

internal interface IDelegateAssertions<TActual, TAnd, TOr>
    where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
    where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
{
    public Throws<TActual, TAnd, TOr> Throws { get; }
}