using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Interfaces;

public interface ISource<TActual, TAnd, TOr> 
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    AssertionBuilder<TActual, TAnd, TOr> AssertionBuilder { get; }
}