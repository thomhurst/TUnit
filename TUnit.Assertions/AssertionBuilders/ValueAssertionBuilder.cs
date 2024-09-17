using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertionBuilders;


public class ValueAssertionBuilder<TActual> 
    : AssertionBuilder<TActual, ValueAnd<TActual>, ValueOr<TActual>>,
        IValueSource<TActual, ValueAnd<TActual>, ValueOr<TActual>>
{
    internal ValueAssertionBuilder(TActual value, string expressionBuilder) : base(value.AsAssertionData(expressionBuilder), expressionBuilder)
    {
    }

    public static InvokableAssertionBuilder<TActual, ValueAnd<TActual>, ValueOr<TActual>> Create(Func<Task<AssertionData<TActual>>> assertionDataDelegate, AssertionBuilder<TActual> assertionBuilder)
    {
        return new InvokableAssertionBuilder<TActual, ValueAnd<TActual>, ValueOr<TActual>>(assertionDataDelegate, (AssertionBuilder<TActual, ValueAnd<TActual>, ValueOr<TActual>>)assertionBuilder);
    }

    AssertionBuilder<TActual, ValueAnd<TActual>, ValueOr<TActual>> ISource<TActual, ValueAnd<TActual>, ValueOr<TActual>>.AssertionBuilder => this;
}