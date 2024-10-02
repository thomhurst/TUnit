using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertionBuilders;

public class AsyncValueDelegateAssertionBuilder<TActual> 
    : AssertionBuilder<TActual>,
        IDelegateSource<TActual>,
        IValueSource<TActual>
 {
    internal AsyncValueDelegateAssertionBuilder(Func<Task<TActual>> function, string expressionBuilder) : base(function.AsAssertionData(expressionBuilder), expressionBuilder)
    {
    }
    
    AssertionBuilder<TActual> ISource<TActual>.AssertionBuilder => this;
 }