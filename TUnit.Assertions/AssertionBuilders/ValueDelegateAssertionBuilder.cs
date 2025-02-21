using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertionBuilders;

public class ValueDelegateAssertionBuilder<TActual> 
    : AssertionBuilder, IValueDelegateSource<TActual>
{
    internal ValueDelegateAssertionBuilder(Func<TActual> function, string? expressionBuilder) : base(function.AsAssertionData(expressionBuilder), expressionBuilder)
    {
    }
}