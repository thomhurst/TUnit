namespace TUnit.Assertions.AssertConditions.Operators;

internal interface IValueAssertions<TActual, TAnd, TOr>
    where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
    where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
{
    public Is<TActual, TAnd, TOr> Is { get; }
    public Does<TActual, TAnd, TOr> Does { get; }
    public Has<TActual, TAnd, TOr> Has { get; }
}