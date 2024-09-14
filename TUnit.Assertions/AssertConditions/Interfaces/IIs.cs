using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertConditions.Interfaces;

public interface IIs<TActual, TAnd, TOr>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    internal Is<TActual, TAnd, TOr> Is();
    internal IsNot<TActual, TAnd, TOr> IsNot();
}