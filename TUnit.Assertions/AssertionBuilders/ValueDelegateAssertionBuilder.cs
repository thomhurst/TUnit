using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.Extensions;
using TUnit.Assertions.Messages;

namespace TUnit.Assertions.AssertionBuilders;

public class ValueDelegateAssertionBuilder<TActual> 
    : AssertionBuilder<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>, 
        IDelegateSource<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>, 
        IValueSource<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>
{
    internal ValueDelegateAssertionBuilder(Func<TActual> function, string expressionBuilder) : base(function.AsAssertionData, expressionBuilder)
    {
    }
    
    public ValueDelegateAssertionBuilder<TActual> WithMessage(AssertionMessageValueDelegate<TActual> message)
    {
        AssertionMessage = message;
        return this;
    }
        
    public ValueDelegateAssertionBuilder<TActual> WithMessage(Func<TActual?, Exception?, string> message)
    {
        AssertionMessage = (AssertionMessageValueDelegate<TActual>) message;
        return this;
    }
    
    public ValueDelegateAssertionBuilder<TActual> WithMessage(Func<string> message)
    {
        AssertionMessage = (AssertionMessageValueDelegate<TActual>) message;
        return this;
    }

    public static InvokableAssertionBuilder<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> Create(Func<Task<AssertionData<TActual>>> assertionDataDelegate, AssertionBuilder<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> assertionBuilder)
    {
        return new InvokableAssertionBuilder<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>(assertionDataDelegate, assertionBuilder);
    }
    
    AssertionBuilder<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> ISource<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>.AssertionBuilder => this;
}