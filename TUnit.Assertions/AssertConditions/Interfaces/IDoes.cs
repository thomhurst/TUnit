using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertConditions.Interfaces;

public interface IDoes<TActual, TAnd, TOr>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    internal Does<TActual, TAnd, TOr> Does();
    internal DoesNot<TActual, TAnd, TOr> DoesNot();
}