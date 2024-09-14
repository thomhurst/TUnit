namespace TUnit.Assertions.AssertConditions.Operators;

public interface IValueAssertions<TActual, TAnd, TOr>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    public Is<TActual, TAnd, TOr> Is { get; }
    public Does<TActual, TAnd, TOr> Does { get; }
    public Has<TActual, TAnd, TOr> Has { get; }
}