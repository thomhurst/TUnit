namespace TUnit.Assertions.AssertConditions.Operators;

public class Or<TActual, TAnd, TOr>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>;