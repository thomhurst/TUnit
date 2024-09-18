using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertionBuilders;

public class AsyncValueDelegateAssertionBuilder<TActual> 
    : AssertionBuilder<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>,
        IDelegateSource<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>,
        IValueSource<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>
 {
    internal AsyncValueDelegateAssertionBuilder(Func<Task<TActual>> function, string expressionBuilder) : base(function.AsAssertionData(expressionBuilder), expressionBuilder)
    {
    }
    
    AssertionBuilder<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>> ISource<TActual, ValueDelegateAnd<TActual>, ValueDelegateOr<TActual>>.AssertionBuilder => this;
 }