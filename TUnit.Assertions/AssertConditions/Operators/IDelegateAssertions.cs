using TUnit.Assertions.AssertConditions.Interfaces;

namespace TUnit.Assertions.AssertConditions.Operators;

public interface IDelegateAssertions<TActual, TAnd, TOr>
: IThrows<TActual, TAnd, TOr>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>;