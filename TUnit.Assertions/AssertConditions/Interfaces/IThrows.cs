using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertConditions.Throws;

namespace TUnit.Assertions.AssertConditions.Interfaces;

public interface IThrows<TActual, TAnd, TOr>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    internal Throws<TActual, TAnd, TOr> Throws();
    
    public ThrowsException<TActual, TAnd, TOr> ThrowsException() => Throws().Exception();
    public BaseAssertCondition<TActual, TAnd, TOr> ThrowsNothing() => Throws().Nothing();
}