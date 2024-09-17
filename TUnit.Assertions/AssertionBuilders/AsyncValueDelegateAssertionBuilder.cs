using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.Extensions;
using TUnit.Assertions.Messages;

namespace TUnit.Assertions.AssertionBuilders;

public class AsyncValueDelegateAssertionBuilder<TActual> 
    : AssertionBuilder<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>,
        IDelegateSource<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>,
        IValueSource<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>
 {
    internal AsyncValueDelegateAssertionBuilder(Func<Task<TActual>> function, string expressionBuilder) : base(function.AsAssertionData(expressionBuilder), expressionBuilder)
    {
    }
    
    public static InvokableAssertionBuilder<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> Create(Func<Task<AssertionData<TActual>>> assertionDataDelegate, AssertionBuilder<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> assertionBuilder)
    {
        return new InvokableAssertionBuilder<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>(
            assertionDataDelegate, assertionBuilder);
    }
    
    AssertionBuilder<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> ISource<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>.AssertionBuilder => this;
 }