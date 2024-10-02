using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertionBuilders;


public class ValueAssertionBuilder<TActual> 
    : AssertionBuilder<TActual>,
        IValueSource<TActual>
{
    internal ValueAssertionBuilder(TActual value, string expressionBuilder) : base(value.AsAssertionData(expressionBuilder), expressionBuilder)
    {
    }

    AssertionBuilder<TActual> ISource<TActual>.AssertionBuilder => this;
}