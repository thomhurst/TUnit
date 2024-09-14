using TUnit.Assertions.AssertConditions.Interfaces;

namespace TUnit.Assertions.AssertConditions.Operators;

public interface IValueAssertions<TActual, TAnd, TOr>
: IIs<TActual, TAnd, TOr>,
    IDoes<TActual, TAnd, TOr>,
    IHas<TActual, TAnd, TOr>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>;