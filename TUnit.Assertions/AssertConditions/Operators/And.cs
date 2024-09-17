namespace TUnit.Assertions.AssertConditions.Operators;

public abstract class And<TActual, TAnd, TOr>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>;