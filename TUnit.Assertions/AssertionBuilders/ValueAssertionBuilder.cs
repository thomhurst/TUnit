using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.Extensions;
using TUnit.Assertions.Messages;

namespace TUnit.Assertions.AssertionBuilders;


public class ValueAssertionBuilder<TActual> 
    : AssertionBuilder<TActual, ValueAnd<TActual>, ValueOr<TActual>>,
        IOutputsChain<NoneAssertionBuilder<TActual, ValueAnd<TActual>, ValueOr<TActual>>, TActual, ValueAnd<TActual>, ValueOr<TActual>>,
        IIs<TActual, ValueAnd<TActual>, ValueOr<TActual>>,
        IHas<TActual, ValueAnd<TActual>, ValueOr<TActual>>,
        IDoes<TActual, ValueAnd<TActual>, ValueOr<TActual>>
{
    private readonly TActual _value;

    AssertionConnector<TActual, ValueAnd<TActual>, ValueOr<TActual>> IAssertionConnector<TActual, ValueAnd<TActual>, ValueOr<TActual>>.AssertionConnector => new(this, ChainType.Or);

    internal ValueAssertionBuilder(TActual value, string expressionBuilder) : base(value.AsAssertionData(), expressionBuilder)
    {
        _value = value;
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

    public static NoneAssertionBuilder<TActual, ValueAnd<TActual>, ValueOr<TActual>> Create(Func<Task<AssertionData<TActual>>> assertionDataDelegate, AssertionBuilder<TActual, ValueAnd<TActual>, ValueOr<TActual>> assertionBuilder)
    {
        return new NoneAssertionBuilder<TActual, ValueAnd<TActual>, ValueOr<TActual>>(assertionDataDelegate, assertionBuilder);
    }
}