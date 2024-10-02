using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Interfaces;

public interface ISource<TActual> 
{
    AssertionBuilder<TActual> AssertionBuilder { get; }
}