using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Operators;

public class ValueDelegateAnd<TActual>(AssertionBuilder<TActual> assertionBuilder) : IValueDelegateSource<TActual>
{
    public static ValueDelegateAnd<TActual> Create(AssertionBuilder<TActual> assertionBuilder)
    {
        return new ValueDelegateAnd<TActual>(assertionBuilder);
    }

    AssertionBuilder<TActual>
        ISource<TActual>.AssertionBuilder => new AndAssertionBuilder<TActual>(assertionBuilder);
}