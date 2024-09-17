using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.Extensions;
using TUnit.Assertions.Messages;

namespace TUnit.Assertions.AssertionBuilders;


public class ValueAssertionBuilder<TActual> 
    : AssertionBuilder<TActual, ValueAnd<TActual>, ValueOr<TActual>>,
        IValueSource<TActual, ValueAnd<TActual>, ValueOr<TActual>>
{
    internal ValueAssertionBuilder(TActual value, string expressionBuilder) : base(value.AsAssertionData(expressionBuilder), expressionBuilder)
    {
    }
    
    public ValueAssertionBuilder<TActual> WithMessage(AssertionMessageValue<TActual> message)
    {
        AssertionMessage = message;
        return this;
    }
    
    public ValueAssertionBuilder<TActual> WithMessage(Func<TActual?, string> message)
    {
        AssertionMessage = (AssertionMessageValue<TActual>) message;
        return this;
    }
    
    public ValueAssertionBuilder<TActual> WithMessage(Func<string> message)
    {
        AssertionMessage = (AssertionMessageValue<TActual>) message;
        return this;
    }

    public static InvokableAssertionBuilder<TActual, ValueAnd<TActual>, ValueOr<TActual>> Create(Func<Task<AssertionData<TActual>>> assertionDataDelegate, AssertionBuilder<TActual> assertionBuilder)
    {
        return new InvokableAssertionBuilder<TActual, ValueAnd<TActual>, ValueOr<TActual>>(assertionDataDelegate, (AssertionBuilder<TActual, ValueAnd<TActual>, ValueOr<TActual>>)assertionBuilder);
    }

    AssertionBuilder<TActual, ValueAnd<TActual>, ValueOr<TActual>> ISource<TActual, ValueAnd<TActual>, ValueOr<TActual>>.AssertionBuilder => this;
}